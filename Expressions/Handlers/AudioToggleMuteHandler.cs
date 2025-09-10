using System.Threading;
using System.Threading.Tasks;

namespace QuickLauncher.Engine.Expressions.Handlers
{
    /// <summary>
    /// Implements the "audio.toggle_mute" verb.  This handler simulates the Volume Mute
    /// key on Windows.  On other platforms it performs no action.
    /// </summary>
    public sealed class AudioToggleMuteHandler : IExpressionHandler
    {
        public string Verb => "audio.toggle_mute";

        public bool RequiresTrustedMode => false;

        public Task<bool> ExecuteAsync(string args, CancellationToken cancellationToken = default)
        {
            // Use SendKeys handler to reuse implementation.  In a full implementation this
            // could call into an audio API for finer control.
            var send = new SendKeysHandler();
            return send.ExecuteAsync("{Volume_Mute}");
        }
    }
}