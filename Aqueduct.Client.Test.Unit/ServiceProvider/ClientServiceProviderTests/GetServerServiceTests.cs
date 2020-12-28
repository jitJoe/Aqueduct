using System;
using Aqueduct.Client.Transport;
using Aqueduct.Shared.Proxy;
using Xunit;

namespace Aqueduct.Client.Test.Unit.ServiceProvider.ClientServiceProviderTests
{
    public class GetServerServiceTests : ClientServiceProviderTestsBase
    {
        [Fact]
        public void Generic_Type_Throws()
        {
            var exception = Assert.Throws<Exception>(() => _clientServiceProvider.GetServerService<IGenericType<string>>());
            
            Assert.Equal("Cannot get generic server service proxy", exception.Message);
        }

        [Fact]
        public void Happy()
        {
            var proxyInvocationHandler = new ProxyInvocationHandler<ClientToServerInvocationMetaData>(null);
            
            _clientTransportDriverMock.Setup(clientTransportDriver => clientTransportDriver.InvocationHandler)
                .Returns(proxyInvocationHandler);

            _proxyProviderMock.Setup(proxyProvider => proxyProvider.GetProxyType<IType, ClientToServerInvocationMetaData>(proxyInvocationHandler))
                .Returns(typeof(TypeProxyImpl));
            
            Assert.NotNull(_clientServiceProvider.GetServerService<IType>());
        }
        
        private interface IGenericType<T>
        {
        
        }

        private interface IType
        {
            
        }

        private class TypeProxyImpl : IType
        {
            private ProxyInvocationHandler<ClientToServerInvocationMetaData> _target;
            private ClientToServerInvocationMetaData _metadata;

            public TypeProxyImpl(ProxyInvocationHandler<ClientToServerInvocationMetaData> target, ClientToServerInvocationMetaData metadata)
            {
                _target = target;
                _metadata = metadata;
            }
        }
    }
}