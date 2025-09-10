using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace QuickLauncher.Engine.Expressions.Handlers
{
    /// <summary>
    /// Implements the "system" verb.  Supports subcommands like "sleep" and "hibernate".
    /// On Windows this uses SetSuspendState from powrprof.dll.  On other platforms it
    /// currently performs no action and returns false.
    /// </summary>
    public sealed class SystemPowerHandler : IExpressionHandler
    {
        public string Verb => "system";

        public bool RequiresTrustedMode => false;

        public Task<bool> ExecuteAsync(string args, CancellationToken cancellationToken = default)
        {
            var arg = args?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(arg))
                return Task.FromResult(false);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (arg)
                {
                    case "sleep":
                        return Task.FromResult(SetSuspendState(false));
                    case "hibernate":
                        return Task.FromResult(SetSuspendState(true));
                    case "restart":
                    case "shutdown":
                        // Use Windows command as fallback.  TODO: Consider ExitWindowsEx or shutdown.exe
                        var action = arg == "restart" ? "/r /t 0" : "/s /t 0";
                        System.Diagnostics.Process.Start("shutdown", action);
                        return Task.FromResult(true);
                    default:
                        return Task.FromResult(false);
                }
            }
            // Unsupported platform
            return Task.FromResult(false);
        }

        private static bool SetSuspendState(bool hibernate)
        {
            try
            {
                // bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent)
                return PowrProf_SetSuspendState(hibernate ? 1 : 0, 1, 0) != 0;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("PowrProf.dll", SetLastError = true)]
        private static extern int PowrProf_SetSuspendState(int hibernate, int forceCritical, int disableWakeEvent);
    }
}