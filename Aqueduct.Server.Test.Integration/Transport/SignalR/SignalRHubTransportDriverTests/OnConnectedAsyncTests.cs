using System;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Aqueduct.Server.Test.Integration.Transport.SignalR.SignalRHubTransportDriverTests
{
    public class OnConnectedAsyncTests : SignalRHubTransportDriverTestsBase
    {
        [Fact]
        public async Task Registers_ConnectionId()
        {
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(Guid.NewGuid()));
                
            await StartServerAndClientAsync();
            
            TestBridge.ConnectionIdMappingRegistryMock.Verify(connectionIdMappingRegistry =>
                connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()));
        }
    }
}