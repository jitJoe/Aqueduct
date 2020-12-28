using System;
using Aqueduct.Client.ServiceProvider;
using Aqueduct.Client.Transport;
using Aqueduct.Shared.Proxy;
using Moq;

namespace Aqueduct.Client.Test.Unit.ServiceProvider.ClientServiceProviderTests
{
    public abstract class ClientServiceProviderTestsBase
    {
        protected readonly Mock<ITypeFinder> _typeFinderMock = new();
        protected readonly Mock<IProxyProvider> _proxyProviderMock = new();
        protected readonly Mock<IClientTransportDriver> _clientTransportDriverMock = new();
        protected readonly Mock<IServiceProvider> _serviceProviderMock = new();

        protected readonly ClientServiceProvider _clientServiceProvider;

        public ClientServiceProviderTestsBase()
        {
            _clientServiceProvider = new ClientServiceProvider(_typeFinderMock.Object, 
                _proxyProviderMock.Object, 
                _clientTransportDriverMock.Object, 
                _serviceProviderMock.Object);
        }
    }
}