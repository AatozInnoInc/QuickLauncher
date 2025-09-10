using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuickLauncher.Engine.Hotkeys;

/// <summary>
/// Windows implementation of <see cref="IHotkeyService"/>.  Registers hotkeys with
/// user32.dll using RegisterHotKey and listens for WM_HOTKEY messages on a hidden
/// form.  This class is intended to demonstrate how hotkey registration might be
/// implemented; production code should handle errors and edge cases more robustly.
/// </summary>
public sealed class WindowsHotkeyService : Form, IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private readonly ConcurrentDictionary<string, int> _actionIds = new();
    private int _nextId = 1;
    public event EventHandler<string>? Triggered;

    public WindowsHotkeyService()
    {
        // Hide the window; we just need it to receive WM_HOTKEY messages.
        this.ShowInTaskbar = false;
        this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        this.Opacity = 0;
    }

    public bool Register(HotkeyBinding binding)
    {
        if (binding == null) throw new ArgumentNullException(nameof(binding));
        if (!binding.Enabled) return false;
        // Compute modifier flags.
        var mod = 0;
        if (binding.Modifiers.HasFlag(Modifiers.Alt)) mod |= MOD_ALT;
        if (binding.Modifiers.HasFlag(Modifiers.Ctrl)) mod |= MOD_CONTROL;
        if (binding.Modifiers.HasFlag(Modifiers.Shift)) mod |= MOD_SHIFT;
        if (binding.Modifiers.HasFlag(Modifiers.Win)) mod |= MOD_WIN;
        var id = Interlocked.Increment(ref _nextId);
        var success = RegisterHotKey(this.Handle, id, mod, (int)binding.Key);
        if (success)
        {
            _actionIds[binding.Action] = id;
        }
        return success;
    }

    public void Unregister(string action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (_actionIds.TryRemove(action, out var id))
        {
            UnregisterHotKey(this.Handle, id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var kvp in _actionIds)
        {
            UnregisterHotKey(this.Handle, kvp.Value);
        }
        _actionIds.Clear();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            var id = m.WParam.ToInt32();
            // Find the action associated with this id.
            foreach (var kvp in _actionIds)
            {
                if (kvp.Value == id)
                {
                    Triggered?.Invoke(this, kvp.Key);
                    break;
                }
            }
        }
        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnregisterAll();
        }
        base.Dispose(disposing);
    }

    // Win32 constants
    private const int MOD_ALT = 0x0001;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int MOD_WIN = 0x0008;
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}