using System;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Aqueduct.Server.Test.Integration.Transport.SignalR.SignalRHubTransportDriverTests
{
    public class OnDisconnectedAsyncTests : SignalRHubTransportDriverTestsBase
    {
        [Fact]
        public async Task Unregisters_ConnectionId()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));

            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                connectionIdMappingRegistry.RemoveConnectionAsync(connectionId));

            await StartServerAndClientAsync();

            await _hubConnection.StopAsync();
            
            TestBridge.ConnectionIdMappingRegistryMock.Verify(connectionIdMappingRegistry =>
                connectionIdMappingRegistry.RemoveConnectionAsync(connectionId));
        }
    }
}