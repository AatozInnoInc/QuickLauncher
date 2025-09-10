namespace QuickLauncher.Engine;

/// <summary>
/// Defines a single launchable command.  A command has a unique identifier, a label
/// (displayed in the UI), an optional category, a command type describing how it should
/// be executed, a raw arguments string, and some metadata (hit count, last run time,
/// custom icon).  Hit count and recency information seed the ranking algorithm to
/// personalise suggestions.
/// </summary>
public sealed class Command
{
    /// <summary>
    /// Unique identifier for the command.  Generated on import or creation.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human‑friendly name for the command.  Typically derived from the INI section
    /// header (e.g. "Restart Computer").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional category or prefix extracted from the label (e.g. "int" or "user").
    /// Categories can be used to scope searches or provide filtering in the UI.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Specifies how to execute the command.  See <see cref="CommandType"/> for details.
    /// </summary>
    public CommandType Type { get; set; } = CommandType.Expression;

    /// <summary>
    /// The verb portion of an expression command (e.g. "send", "system.sleep").  For
    /// <see cref="CommandType.Run"/> commands this is ignored.  For built‑in commands
    /// this is also ignored; built‑ins are resolved by the Label.
    /// </summary>
    public string Verb { get; set; } = string.Empty;

    /// <summary>
    /// Arguments passed to the expression handler or process.  For Run commands this
    /// might be a path, URI, or command line; for expressions it is the remainder of
    /// the expression after the verb.
    /// </summary>
    public string Args { get; set; } = string.Empty;

    /// <summary>
    /// Optional alternative names that can be used to refer to this command in search.
    /// </summary>
    public string[] Aliases { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Seeds the frequency component of the ranking algorithm.  The engine should update
    /// this value when a command is run.
    /// </summary>
    public int HitCount { get; set; } = 0;

    /// <summary>
    /// Tracks the most recent time this command was executed.  Used by the recency
    /// component of the ranking algorithm.
    /// </summary>
    public DateTimeOffset? LastRunUtc { get; set; } = null;

    /// <summary>
    /// Optional file path to a custom icon.  The UI can display this icon instead of
    /// deriving one from the target (e.g. the file's icon or a generic glyph).
    /// </summary>
    public string? IconPath { get; set; }
        = null;

    /// <summary>
    /// Indicates whether the command is built in (internal).  Built‑ins cannot be
    /// removed or renamed by the user.
    /// </summary>
    public bool IsBuiltIn { get; set; } = false;
}