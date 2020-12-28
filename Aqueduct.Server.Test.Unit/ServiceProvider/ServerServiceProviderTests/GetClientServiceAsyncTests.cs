using System;
using System.Threading.Tasks;
using Aqueduct.Server.Transport;
using Aqueduct.Shared.Proxy;
using Moq;
using Xunit;

namespace Aqueduct.Server.Test.Unit.ServiceProvider.ServerServiceProviderTests
{
    public class GetClientServiceAsyncTests : ServerServiceProviderTestsBase
    {
        [Fact]
        public async Task Creates_Instance_Of_Proxy_Type()
        {
            var invocationHandler = new ProxyInvocationHandler<ServerToClientInvocationMetaData>(null);

            _serverTransportDriverMock.Setup(serverTransportDriverMock => serverTransportDriverMock.InvocationHandler)
                .Returns(invocationHandler);
            
            _proxyProvider.Setup(proxyProvider => proxyProvider.GetProxyType<IType, ServerToClientInvocationMetaData>(invocationHandler))
                .Returns(typeof(ITypeProxy));

            var connectionId = Guid.NewGuid();
            
            var proxyType = await _serverServiceProvider.GetClientServiceAsync<IType>(connectionId);

            Assert.IsType<ITypeProxy>(proxyType);
            Assert.Equal(invocationHandler, (proxyType as ITypeProxy).InvocationHandler);
            Assert.Equal(connectionId.ToString(), (proxyType as ITypeProxy).ServerToClientInvocationMetaData.AqueductConnectionId);
        }

        private interface IType
        {
        
        }

        private class ITypeProxy : IType
        {
            public ProxyInvocationHandler<ServerToClientInvocationMetaData> InvocationHandler { get; set; }
            public ServerToClientInvocationMetaData ServerToClientInvocationMetaData { get; set; }

            public ITypeProxy(ProxyInvocationHandler<ServerToClientInvocationMetaData> invocationHandler, ServerToClientInvocationMetaData serverToClientInvocationMetaData)
            {
                InvocationHandler = invocationHandler;
                ServerToClientInvocationMetaData = serverToClientInvocationMetaData;
            }
        }
    }
}