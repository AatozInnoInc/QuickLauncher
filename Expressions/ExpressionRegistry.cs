using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuickLauncher.Engine.Expressions
{
    /// <summary>
    /// Manages a set of expression handlers and exposes a method to parse and execute
    /// expressions.  Expressions consist of a verb followed by an optional argument
    /// string.  Handlers are registered via constructor injection.
    /// </summary>
    public sealed class ExpressionRegistry
    {
        private readonly Dictionary<string, IExpressionHandler> _handlers;

        /// <summary>
        /// Creates a new registry with the specified handlers.  If two handlers expose
        /// the same verb, an <see cref="ArgumentException"/> will be thrown.
        /// </summary>
        /// <param name="handlers">The handlers to register.</param>
        public ExpressionRegistry(IEnumerable<IExpressionHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));
            _handlers = new Dictionary<string, IExpressionHandler>(StringComparer.OrdinalIgnoreCase);
            foreach (var handler in handlers)
            {
                if (_handlers.ContainsKey(handler.Verb))
                    throw new ArgumentException($"Duplicate expression verb: {handler.Verb}", nameof(handlers));
                _handlers[handler.Verb] = handler;
            }
        }

        /// <summary>
        /// Gets a handler by verb.  Returns false if no handler is registered for the
        /// specified verb.
        /// </summary>
        public bool TryGetHandler(string verb, out IExpressionHandler? handler)
        {
            if (verb == null) throw new ArgumentNullException(nameof(verb));
            return _handlers.TryGetValue(verb, out handler);
        }

        /// <summary>
        /// Executes an expression by dispatching to the appropriate handler.  If the verb
        /// is unknown or the handler requires trusted mode and trusted mode is disabled
        /// (<paramref name="trustedMode"/> is false), the method returns false.
        /// </summary>
        /// <param name="expression">The raw expression string (verb and arguments).</param>
        /// <param name="trustedMode">Indicates whether commands requiring trusted mode
        /// are allowed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the command executed successfully; otherwise false.</returns>
        public async Task<bool> ExecuteAsync(string expression, bool trustedMode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;
            // Split at first whitespace to separate verb and arguments.
            var span = expression.AsSpan().Trim();
            var verbEnd = span.IndexOfAny(' ', '\t');
            ReadOnlySpan<char> verbSpan;
            ReadOnlySpan<char> argsSpan;
            if (verbEnd < 0)
            {
                verbSpan = span;
                argsSpan = ReadOnlySpan<char>.Empty;
            }
            else
            {
                verbSpan = span.Slice(0, verbEnd);
                argsSpan = span.Slice(verbEnd + 1).Trim();
            }
            var verb = verbSpan.ToString();
            var args = argsSpan.ToString();

            if (!TryGetHandler(verb, out var handler))
                return false;
            if (handler.RequiresTrustedMode && !trustedMode)
                return false;
            try
            {
                return await handler.ExecuteAsync(args, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }
    }
}