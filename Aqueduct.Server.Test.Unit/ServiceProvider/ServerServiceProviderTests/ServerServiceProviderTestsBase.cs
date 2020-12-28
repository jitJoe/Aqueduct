using System;
using Aqueduct.Server.ServiceProvider;
using Aqueduct.Server.Transport;
using Aqueduct.Server.Transport.SignalR;
using Aqueduct.Shared.Proxy;
using Moq;

namespace Aqueduct.Server.Test.Unit.ServiceProvider.ServerServiceProviderTests
{
    public abstract class ServerServiceProviderTestsBase
    {
        protected readonly Mock<ITypeFinder> _typeFinderMock = new();
        protected readonly Mock<IServiceProvider> _serviceProviderMock = new();
        protected readonly Mock<IConnectionIdMappingRegistry> _connectionIdMappingRegistry = new();
        protected readonly Mock<IProxyProvider> _proxyProvider = new();
        protected readonly Mock<IServerTransportDriver> _serverTransportDriverMock = new();
        
        protected readonly ServerServiceProvider _serverServiceProvider;

        protected ServerServiceProviderTestsBase()
        {
            _serverServiceProvider = new ServerServiceProvider(_typeFinderMock.Object,
                _serviceProviderMock.Object,
                _connectionIdMappingRegistry.Object,
                _proxyProvider.Object,
                _serverTransportDriverMock.Object);
        }
    }
}