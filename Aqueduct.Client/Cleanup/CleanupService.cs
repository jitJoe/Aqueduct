using System.Timers;
using Aqueduct.Shared.CallbackRegistry;

namespace Aqueduct.Client.Cleanup
{
    public class CleanupService
    {
        private readonly ICallbackRegistry _callbackRegistry;
        private readonly Timer _cleanupTimer;

        public CleanupService(ICallbackRegistry callbackRegistry)
        {
            _callbackRegistry = callbackRegistry;
            _cleanupTimer = new Timer(500);
            _cleanupTimer.Elapsed += (_, _) => _callbackRegistry.ClearExpiredCallbacks();
        }
    }
}