using System;

namespace QuickLauncher.Engine.Hotkeys
{
    /// <summary>
    /// Flags enumeration representing modifier keys used for registering global hotkeys.
    /// </summary>
    [Flags]
    public enum Modifiers
    {
        None = 0,
        Alt = 1,
        Ctrl = 2,
        Shift = 4,
        Win = 8
    }
}