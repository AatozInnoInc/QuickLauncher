using System.Windows.Forms;

namespace QuickLauncher.Engine.Hotkeys;

/// <summary>
/// Describes a single hotkey binding.  The hotkey is defined by a set of modifiers and a
/// primary key; when pressed, it triggers the associated action name.  Disabled bindings
/// are ignored by the hotkey service.
/// </summary>
public sealed record HotkeyBinding(string Action, Modifiers Modifiers, Keys Key, bool Enabled = true);