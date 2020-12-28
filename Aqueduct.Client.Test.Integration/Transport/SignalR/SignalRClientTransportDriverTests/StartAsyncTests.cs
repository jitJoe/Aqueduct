using System;
using Xunit;

namespace Aqueduct.Client.Test.Integration.Transport.SignalR.SignalRClientTransportDriverTests
{
    public class StartAsyncTests : SignalRClientTransportDriverTestsBase
    {
        [Fact]
        public async void Cannot_Connect_Throws()
        {
            var exception = await Assert.ThrowsAsync<Exception>(async () => await _signalRClientTransportDriver.StartAsync());
            
            Assert.Equal("Cannot connect to Server", exception.Message);
        }
        
        [Fact]
        public async void Connect()
        {
            StartServer();

            await _signalRClientTransportDriver.StartAsync();

            Assert.Equal(1, _testHubAccessor.ConnectedCount);
        }
        
        [Fact]
        public async void Subsequent_Connect_Does_Not_Open_Second_Hub_Connection()
        {
            StartServer();

            await _signalRClientTransportDriver.StartAsync();

            Assert.Equal(1, _testHubAccessor.ConnectedCount);
            
            await _signalRClientTransportDriver.StartAsync();
            
            Assert.Equal(1, _testHubAccessor.ConnectedCount);
        }
    }
}