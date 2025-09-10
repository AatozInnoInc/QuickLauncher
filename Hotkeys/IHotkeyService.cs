using System;

namespace QuickLauncher.Engine.Hotkeys
{
    /// <summary>
    /// Defines an interface for managing global hotkey bindings.  Implementations should
    /// register and unregister hotkeys with the operating system and raise events when
    /// hotkeys are triggered.  All methods except Dispose should be thread‑safe.
    /// </summary>
    public interface IHotkeyService : IDisposable
    {
        /// <summary>
        /// Attempts to register the specified binding.  Returns false if the binding
        /// conflicts with an existing registration or cannot be registered.  If
        /// <paramref name="binding"/> is disabled, registration is skipped and false is
        /// returned.
        /// </summary>
        bool Register(HotkeyBinding binding);

        /// <summary>
        /// Unregisters the hotkey associated with the specified action, if any.
        /// </summary>
        void Unregister(string action);

        /// <summary>
        /// Unregisters all hotkeys.
        /// </summary>
        void UnregisterAll();

        /// <summary>
        /// Occurs when a registered hotkey is triggered.  The argument is the action name
        /// associated with the binding.
        /// </summary>
        event EventHandler<string>? Triggered;
    }
}