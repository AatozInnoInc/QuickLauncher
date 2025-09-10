namespace QuickLauncher.Engine;

/// <summary>
/// Represents the type of action a command performs.  The legacy AutoHotKey implementation
/// encoded the action in a `Func` string in the INI file; the importer maps that string to
/// one of these values.  Extending this enumeration will require updating the importer
/// accordingly.
/// </summary>
public enum CommandType
{
    /// <summary>
    /// Launches a process, opens a folder, or navigates to a URI using the shell.
    /// </summary>
    Run,

    /// <summary>
    /// Executes a command via an expression verb (e.g. send, system.sleep).  This covers
    /// what the original AutoHotKey implementation called `DynaExpr_Eval`.  All
    /// expressions must resolve to a registered verb in the expression registry.
    /// </summary>
    Expression,

    /// <summary>
    /// Executes a built‑in command provided by the engine (such as exit, reload, or
    /// opening the hotkey settings).  Built‑ins are mapped by their label and not
    /// extensible by users.
    /// </summary>
    BuiltIn
}