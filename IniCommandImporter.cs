using System.Globalization;
using System.Text;

namespace QuickLauncher.Engine;

/// <summary>
/// Provides methods to import commands from the legacy `.ini` files used by the
/// AutoHotKey QuickLauncher.  Each section in the INI is interpreted as a command; the
/// section name becomes the label and category, the <c>Func</c> key maps to
/// <see cref="CommandType"/>, and the <c>Parms</c> key becomes the arguments.  The
/// <c>HitCount</c> key seeds the frequency component of the ranking algorithm.
/// </summary>
public static class IniCommandImporter
{
    /// <summary>
    /// Reads an INI file and returns a sequence of commands.  Sections without a
    /// <c>Func</c> key are ignored.  Unknown function names default to <see cref="CommandType.Run"/>.
    /// </summary>
    /// <param name="path">Absolute or relative path to the INI file.</param>
    /// <returns>A list of <see cref="Command"/> objects representing the contents of the file.</returns>
    public static IEnumerable<Command> Import(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("INI path must not be null or whitespace.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"INI file not found: {path}", path);

        // Simple state machine: track current section name and accumulate key/value pairs.
        var currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? sectionName = null;

        foreach (var rawLine in File.ReadLines(path, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(";"))
                continue; // Skip empty lines and comments.

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                // Emit the previous section as a command if appropriate.
                if (sectionName != null)
                {
                    if (currentSection.TryGetValue("Func", out var func))
                    {
                        yield return CreateCommand(sectionName, currentSection);
                    }

                    currentSection.Clear();
                }
                sectionName = line.Substring(1, line.Length - 2);
                continue;
            }

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex > 0)
            {
                var key = line.Substring(0, equalsIndex).Trim();
                var value = line.Substring(equalsIndex + 1).Trim();
                currentSection[key] = value;
            }
        }

        // Emit the last section.
        if (sectionName != null && currentSection.ContainsKey("Func"))
        {
            yield return CreateCommand(sectionName, currentSection);
        }
    }

    private static Command CreateCommand(string rawLabel, Dictionary<string, string> entries)
    {
        // Extract category and clean label.  If the label contains a colon, treat the
        // substring before the colon as the category.  Otherwise category is empty.
        string category = string.Empty;
        string label = rawLabel;
        var colonIndex = rawLabel.IndexOf(':');
        if (colonIndex > 0)
        {
            category = rawLabel.Substring(0, colonIndex).Trim();
            label = rawLabel.Substring(colonIndex + 1).Trim();
        }

        var func = entries.TryGetValue("Func", out var f) ? f : "Run";
        var parms = entries.TryGetValue("Parms", out var p) ? p : string.Empty;
        var hitCountStr = entries.TryGetValue("HitCount", out var h) ? h : null;
        int hitCount = 0;
        if (hitCountStr != null && int.TryParse(hitCountStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedHits))
        {
            hitCount = parsedHits;
        }

        var type = MapFuncToCommandType(func);
        // Initialise verb/args for expression commands.
        string verb = string.Empty;
        string args = string.Empty;
        if (type == CommandType.Run)
        {
            // For run commands we treat the entire parameter as Args.
            args = parms;
        }
        else if (type == CommandType.Expression)
        {
            // Heuristic: if Parms starts with Send, extract verb and arg accordingly.
            var temp = parms.Trim();
            if (temp.StartsWith("Send ", System.StringComparison.OrdinalIgnoreCase))
            {
                verb = "send";
                args = temp.Substring(5).Trim();
            }
            else if (temp.StartsWith("DllCall", System.StringComparison.OrdinalIgnoreCase))
            {
                // Detect SetSuspendState calls and map to sleep/hibernate.
                if (temp.Contains("SetSuspendState") && temp.Contains("int", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Sleep when first int arg is 0, hibernate when first int arg is 1.
                    verb = "system";
                    // Extract first int parameter: look for "\"int\", 0" or 1
                    var firstIntIndex = temp.IndexOf(", \"int\", ", System.StringComparison.OrdinalIgnoreCase);
                    if (firstIntIndex >= 0)
                    {
                        var valueStart = firstIntIndex + 11;
                        var valueChar = temp[valueStart];
                        verb = "system";
                        args = valueChar == '1' ? "hibernate" : "sleep";
                    }
                    else
                    {
                        verb = "system";
                        args = "sleep";
                    }
                }
            }
            else
            {
                // Unknown expression: treat the whole string as verb + args splitted by first space.
                var idx = temp.IndexOf(' ');
                if (idx < 0)
                {
                    verb = temp.ToLowerInvariant();
                    args = string.Empty;
                }
                else
                {
                    verb = temp.Substring(0, idx).ToLowerInvariant();
                    args = temp.Substring(idx + 1);
                }
            }
        }
        else if (type == CommandType.BuiltIn)
        {
            // Built‑ins use parms as args.
            args = parms;
        }

        return new Command
        {
            Label = label,
            Category = category,
            Type = type,
            Verb = verb,
            Args = args,
            HitCount = hitCount,
            IsBuiltIn = IsInternalCommand(category)
        };
    }

    /// <summary>
    /// Maps the INI `Func` string to a <see cref="CommandType"/>.  Unknown values default
    /// to <see cref="CommandType.Run"/>.
    /// </summary>
    private static CommandType MapFuncToCommandType(string func)
    {
        switch (func)
        {
            case "Run":
                return CommandType.Run;
            case "DynaExpr_Eval":
            case "PowerShell":
                return CommandType.Expression;
            case "ExitApp":
            case "Reload":
            case "EditLaunchHK":
            case "EditFlyout":
            case "RestartComputer":
            case "HibernateComputer":
            case "SleepComputer":
                return CommandType.BuiltIn;
            default:
                return CommandType.Run;
        }
    }

    /// <summary>
    /// Determines whether a command should be non‑removable based on its category.  In the
    /// legacy INI files internal commands used the `int:` prefix.  You can customise this
    /// behaviour as needed.
    /// </summary>
    private static bool IsInternalCommand(string category)
    {
        return !string.IsNullOrEmpty(category) && string.Equals(category, "int", StringComparison.OrdinalIgnoreCase);
    }
}