using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Aqueduct.Server.Transport;
using Aqueduct.Shared.Proxy;
using Xunit;

namespace Aqueduct.Server.Test.Unit.ServiceProvider.ServerServiceProviderTests
{
    public class GetClientServiceForAllConnectionsAsyncTests : ServerServiceProviderTestsBase
    {
        [Fact]
        public async Task Creates_Instances_Of_Proxy_Type()
        {
            var invocationHandler = new ProxyInvocationHandler<ServerToClientInvocationMetaData>(null);

            _serverTransportDriverMock.Setup(serverTransportDriverMock => serverTransportDriverMock.InvocationHandler)
                .Returns(invocationHandler);
            
            _proxyProvider.Setup(proxyProvider => proxyProvider.GetProxyType<IType, ServerToClientInvocationMetaData>(invocationHandler))
                .Returns(typeof(ITypeProxy));

            var connectionIdOne = Guid.NewGuid();
            var connectionIdTwo = Guid.NewGuid();
            
            _connectionIdMappingRegistry.Setup(connectionIdMappingRegistry => connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync())
                .Returns(Task.FromResult(new List<Guid> { connectionIdOne, connectionIdTwo }.ToImmutableList()));
            
            var proxyTypes = await _serverServiceProvider.GetClientServiceForAllConnectionsAsync<IType>();

            Assert.Equal(2, proxyTypes.Count);
            
            Assert.IsType<ITypeProxy>(proxyTypes.First());
            Assert.Equal(invocationHandler, (proxyTypes.First() as ITypeProxy).InvocationHandler);
            Assert.Equal(connectionIdOne.ToString(), (proxyTypes.First() as ITypeProxy).ServerToClientInvocationMetaData.AqueductConnectionId);
            
            Assert.IsType<ITypeProxy>(proxyTypes.Last());
            Assert.Equal(invocationHandler, (proxyTypes.Last() as ITypeProxy).InvocationHandler);
            Assert.Equal(connectionIdTwo.ToString(), (proxyTypes.Last() as ITypeProxy).ServerToClientInvocationMetaData.AqueductConnectionId);
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