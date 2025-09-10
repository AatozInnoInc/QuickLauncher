using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QuickLauncher.Engine.Expressions.Handlers
{
    /// <summary>
    /// Implements the "powershell" verb.  This handler executes a PowerShell command in
    /// a separate process.  Because of the inherent risks of arbitrary script
    /// execution, this handler sets <see cref="RequiresTrustedMode"/> to true.  You
    /// should ensure that trusted mode is enabled before registering or calling this
    /// handler.
    /// </summary>
    public sealed class PowerShellHandler : IExpressionHandler
    {
        public string Verb => "powershell";

        public bool RequiresTrustedMode => true;

        public async Task<bool> ExecuteAsync(string args, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(args))
                return false;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -NonInteractive -Command \"{args}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null)
                    return false;
                // Impose a timeout to prevent long‑running scripts.  You can adjust this value.
                var timeoutMs = 5000;
                var exited = await Task.Run(() => proc.WaitForExit(timeoutMs), cancellationToken);
                if (!exited)
                {
                    try { proc.Kill(); } catch { /* ignore */ }
                    return false;
                }
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}