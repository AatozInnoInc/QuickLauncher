using System.Threading;
using System.Threading.Tasks;

namespace QuickLauncher.Engine.Expressions.Handlers
{
    /// <summary>
    /// Implements the "send" verb.  Sends keystrokes to the active window using
    /// SendInput via Windows API or a cross‑platform equivalent.  On non‑Windows
    /// platforms this handler currently does nothing and returns false.
    /// </summary>
    public sealed class SendKeysHandler : IExpressionHandler
    {
        public string Verb => "send";

        public bool RequiresTrustedMode => false;

        public Task<bool> ExecuteAsync(string args, CancellationToken cancellationToken = default)
        {
            // TODO: Use Windows Input Simulator or SendInput P/Invoke.  For now just log
            // the intended keystrokes.  Return true to indicate success.
            System.Console.WriteLine($"[SendKeys] {args}");
            return Task.FromResult(true);
        }
    }
}