using System;
using System.Threading;
using System.Threading.Tasks;
using Aqueduct.Server.Cleanup;
using Aqueduct.Shared.CallbackRegistry;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aqueduct.Server.Test.Unit.Cleanup.CleanupHostedServiceTests
{
    public class ExecuteAsyncTests
    {
        private readonly Mock<ICallbackRegistry> _callbackRegistryMock = new();
        private readonly Mock<ILogger<CleanUpHostedService>> _loggerMock = new();
        private readonly CleanUpHostedService _cleanupHostedService;

        public ExecuteAsyncTests()
        {
            _cleanupHostedService = new CleanUpHostedService(_callbackRegistryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async void Expiry_Tokens_Cleared_Every_500ms_Until_Cancellation_Token_Signalled()
        {
            var cancellationToken = new CancellationToken();

            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.ClearExpiredCallbacks());

            Task.Run(() => _cleanupHostedService.StartAsync(cancellationToken));

            await Task.Delay(2_000);
            
            _callbackRegistryMock.Verify(callbackRegistry => callbackRegistry.ClearExpiredCallbacks(), Times.AtLeast(3));
        }
        
        [Fact]
        public async void Exception_Does_Not_Propagate()
        {
            var cancellationToken = new CancellationToken();

            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.ClearExpiredCallbacks())
                .Throws(new Exception("Unable to clear Callbacks"));

            Task.Run(() => _cleanupHostedService.StartAsync(cancellationToken));

            await Task.Delay(2_000);
            
            _callbackRegistryMock.Verify(callbackRegistry => callbackRegistry.ClearExpiredCallbacks(), Times.AtLeast(3));
        }
    }
}