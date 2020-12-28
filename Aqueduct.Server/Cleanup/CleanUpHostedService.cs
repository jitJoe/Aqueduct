using System;
using System.Threading;
using System.Threading.Tasks;
using Aqueduct.Shared.CallbackRegistry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aqueduct.Server.Cleanup
{
    public class CleanUpHostedService : BackgroundService
    {
        private readonly ICallbackRegistry _callbackRegistry;
        private readonly ILogger<CleanUpHostedService> _logger;

        public CleanUpHostedService(ICallbackRegistry callbackRegistry, ILogger<CleanUpHostedService> logger)
        {
            _callbackRegistry = callbackRegistry;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _callbackRegistry.ClearExpiredCallbacks();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Exception clearing expired callbacks");
                }

                await Task.Delay(500, stoppingToken);
            }
        }
    }
}