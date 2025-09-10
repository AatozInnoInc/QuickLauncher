using System.Threading;
using System.Threading.Tasks;

namespace QuickLauncher.Engine.Expressions
{
    /// <summary>
    /// Defines a verb handler used by the expression registry.  Each handler exposes
    /// a verb string (e.g. "send", "system.sleep") and an asynchronous execute method.
    /// Handlers can indicate whether they require trusted mode, which gates potentially
    /// dangerous operations (e.g. running arbitrary PowerShell).
    /// </summary>
    public interface IExpressionHandler
    {
        /// <summary>
        /// Gets the verb handled by this implementation.  Verbs are case‑insensitive and
        /// should be unique across all registered handlers.
        /// </summary>
        string Verb { get; }

        /// <summary>
        /// Indicates whether execution of this handler requires trusted mode.  If
        /// <c>true</c> the registry will refuse to run the command unless the caller
        /// explicitly enables trusted mode.
        /// </summary>
        bool RequiresTrustedMode { get; }

        /// <summary>
        /// Executes the handler asynchronously.  Implementations should catch and handle
        /// their own exceptions; the return value indicates success or failure.
        /// </summary>
        /// <param name="args">The argument string following the verb.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns><c>true</c> if execution succeeded; otherwise <c>false</c>.</returns>
        Task<bool> ExecuteAsync(string args, CancellationToken cancellationToken = default);
    }
}