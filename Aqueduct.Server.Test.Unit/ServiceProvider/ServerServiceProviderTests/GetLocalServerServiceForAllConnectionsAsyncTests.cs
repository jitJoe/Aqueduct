using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Aqueduct.Server.Test.Unit.ServiceProvider.ServerServiceProviderTests
{
    public class GetLocalServerServiceForAllConnectionsAsyncTests : ServerServiceProviderTestsBase
    {
        private async Task ForBothCallStylesAsync<TService, TLocalService>(Func<Func<Task<List<TLocalService>>>, Task> test) 
            where TService : class where TLocalService : class
        {
            try
            {
                await test(async () => await _serverServiceProvider.GetLocalServerServiceForAllConnectionsAsync<TService, TLocalService>());
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with generic style", exception);
            }

            try
            {
                await test(async () =>
                {
                    var services = await _serverServiceProvider.GetLocalServerServiceForAllConnectionsAsync(typeof(TService), typeof(TLocalService));

                    return services.Select(service => service as TLocalService).ToList();
                });
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with Type reference", exception);
            }
        }
        
        [Fact]
        public async Task Unbound_Generic_Type_Throws()
        {
            _connectionIdMappingRegistry.Setup(connectionIdMappingRegistry => connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync())
                .Returns(Task.FromResult(new List<Guid> { Guid.NewGuid() }.ToImmutableList()));
            
            var exception = await Assert.ThrowsAsync<Exception>(() => 
                _serverServiceProvider.GetLocalServerServiceForAllConnectionsAsync(typeof(IType), typeof(IGenericType<>)));
            
            Assert.Equal("Cannot create instance of generic server type", exception.Message);
        }
        
        [Fact]
        public async Task Bound_Generic_Type_Throws()
        {
            _connectionIdMappingRegistry.Setup(connectionIdMappingRegistry => connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync())
                .Returns(Task.FromResult(new List<Guid> { Guid.NewGuid() }.ToImmutableList()));
            
            await ForBothCallStylesAsync<IType, IGenericType<string>>(async (call) =>
            {
                var exception = await Assert.ThrowsAsync<Exception>(call);
            
                Assert.Equal("Cannot create instance of generic server type", exception.Message);
            });
        }
        
        [Fact]
        public async Task No_Implementation_Registered_With_TypeFinder_Throws()
        {
            _connectionIdMappingRegistry.Setup(connectionIdMappingRegistry => connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync())
                .Returns(Task.FromResult(new List<Guid> { Guid.NewGuid() }.ToImmutableList()));
            
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns((Type) null);
            
            await ForBothCallStylesAsync<IType, ILocalType>(async (call) =>
            {
                var exception = await Assert.ThrowsAsync<Exception>(call);
            
                Assert.StartsWith("Cannot find implementation for", exception.Message);
            });
        }
        
        [Fact]
        public async Task Implementation_Registered_With_TypeFinder_Does_Not_Derive_From_Server_Service_Throws()
        {
            _connectionIdMappingRegistry.Setup(connectionIdMappingRegistry => connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync())
                .Returns(Task.FromResult(new List<Guid> { Guid.NewGuid() }.ToImmutableList()));
            
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(NonDerivedITypeImpl));
            
            await ForBothCallStylesAsync<IType, ILocalType>(async (call) =>
            {
                var exception = await Assert.ThrowsAsync<Exception>(call);

                Assert.EndsWith("as implementation does not derive from ServerService", exception.Message);
            });
        }
        
        [Fact]
        public async Task No_Constructor_Injection()
        {
            _connectionIdMappingRegistry.Setup(connectionIdMappingRegistry => connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync())
                .Returns(Task.FromResult(new List<Guid> { Guid.NewGuid() }.ToImmutableList()));
            
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(DerivedITypeImpl));
            
            await ForBothCallStylesAsync<IType, ILocalType>(async (call) =>
            {
                var serverServices = await call();
                Assert.Single(serverServices);
                
                Assert.IsType<DerivedITypeImpl>(serverServices.First());
            });
        }
        
        [Fact]
        public async Task Constructor_Injection()
        {
            _connectionIdMappingRegistry.Setup(connectionIdMappingRegistry => connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync())
                .Returns(Task.FromResult(new List<Guid> { Guid.NewGuid() }.ToImmutableList()));
            
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(DerivedITypeImplWithConstructorParameters));
            
            var injectedService = new InjectedService();
            
            _serviceProviderMock.Setup(serviceProvider => serviceProvider.GetService(typeof(IInjectedService)))
                .Returns(injectedService);
            
            await ForBothCallStylesAsync<IType, ILocalType>(async (call) =>
            {
                var serverServices = await call();
                Assert.Single(serverServices);

                Assert.IsType<DerivedITypeImplWithConstructorParameters>(serverServices.First());
                Assert.Same(injectedService, ((DerivedITypeImplWithConstructorParameters) serverServices.First()).InjectedService);
            });
        }
        
        [Fact]
        public async Task Constructor_Injection_Multiple_Connections()
        {
            _connectionIdMappingRegistry.Setup(connectionIdMappingRegistry => connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync())
                .Returns(Task.FromResult(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }.ToImmutableList()));
            
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(DerivedITypeImplWithConstructorParameters));
            
            var injectedService = new InjectedService();
            
            _serviceProviderMock.Setup(serviceProvider => serviceProvider.GetService(typeof(IInjectedService)))
                .Returns(injectedService);
            
            await ForBothCallStylesAsync<IType, ILocalType>(async (call) =>
            {
                var serverServices = await call();
                Assert.Equal(2, serverServices.Count);

                Assert.IsType<DerivedITypeImplWithConstructorParameters>(serverServices.First());
                Assert.Same(injectedService, ((DerivedITypeImplWithConstructorParameters) serverServices.First()).InjectedService);
                
                Assert.IsType<DerivedITypeImplWithConstructorParameters>(serverServices.Last());
                Assert.Same(injectedService, ((DerivedITypeImplWithConstructorParameters) serverServices.Last()).InjectedService);
                
                Assert.NotEqual(serverServices.First(), serverServices.Last());
            });
        }
        
        private interface IGenericType<T>
        {
            
        }

        private interface IType
        {
        
        }

        private interface ILocalType
        {
            
        }

        private class NonDerivedITypeImpl : IType, ILocalType
        {
        
        }

        private class DerivedITypeImpl : ServerService, IType, ILocalType
        {
        
        }

        public interface IInjectedService
        {
        
        }

        private class InjectedService : IInjectedService
        {
            
        }

        private class DerivedITypeImplWithConstructorParameters : ServerService, IType, ILocalType
        {
            public IInjectedService InjectedService { get; private set; }

            public DerivedITypeImplWithConstructorParameters(IInjectedService injectedService)
            {
                InjectedService = injectedService;
            }
        }
    }
}