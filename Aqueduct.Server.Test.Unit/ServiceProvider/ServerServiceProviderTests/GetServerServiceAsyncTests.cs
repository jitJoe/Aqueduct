using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Aqueduct.Server.Test.Unit.ServiceProvider.ServerServiceProviderTests
{
    public class GetServerServiceAsyncTests : ServerServiceProviderTestsBase
    {
        private async Task ForBothCallStylesAsync<T>(Guid connectionId, Func<Func<Task<object>>, Task> test) where T : class
        {
            try
            {
                await test(async () => await _serverServiceProvider.GetServerServiceAsync<T>(connectionId));
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with generic style", exception);
            }

            try
            {
                await test(() => _serverServiceProvider.GetServerServiceAsync(typeof(T), connectionId));
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with Type reference", exception);
            }
        }
        
        [Fact]
        public async void Unbound_Generic_Type_Throws()
        {
            var exception = await Assert.ThrowsAsync<Exception>(async () => 
                await _serverServiceProvider.GetServerServiceAsync(typeof(IGenericType<>), Guid.NewGuid()));
            
            Assert.Equal("Cannot create instance of generic server type", exception.Message);
        }
        
        [Fact]
        public async Task Bound_Generic_Type_Throws()
        {
            await ForBothCallStylesAsync<IGenericType<string>>(Guid.NewGuid(), async (call) =>
            {
                var exception = await Assert.ThrowsAsync<Exception>(call);
            
                Assert.Equal("Cannot create instance of generic server type", exception.Message);
            });
        }
        
        [Fact]
        public async Task No_Implementation_Registered_With_TypeFinder_Throws()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns((Type) null);
            
            await ForBothCallStylesAsync<IType>(Guid.NewGuid(), async (call) =>
            {
                var exception = await Assert.ThrowsAsync<Exception>(call);
            
                Assert.StartsWith("Cannot find implementation for", exception.Message);
            });
        }
        
        [Fact]
        public async Task Implementation_Registered_With_TypeFinder_Does_Not_Derive_From_Server_Service_Throws()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns(typeof(NonDerivedITypeImpl));
            
            await ForBothCallStylesAsync<IType>(Guid.NewGuid(), async (call) =>
            {
                var exception = await Assert.ThrowsAsync<Exception>(call);
            
                Assert.EndsWith("as implementation does not derive from ServerService", exception.Message);
            });
        }
        
        [Fact]
        public async Task No_Constructor_Injection()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns(typeof(DerivedITypeImpl));
            
            await ForBothCallStylesAsync<IType>(Guid.NewGuid(), async (call) =>
            {
                Assert.IsType<DerivedITypeImpl>(await call());
            });
        }
        
        [Fact]
        public async Task Constructor_Injection()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns(typeof(DerivedITypeImplWithConstructorParameters));

            var injectedService = new InjectedService();
                
            _serviceProviderMock.Setup(serviceProvider => serviceProvider.GetService(typeof(IInjectedService)))
                .Returns(injectedService);

            
            await ForBothCallStylesAsync<IType>(Guid.NewGuid(), async (call) =>
            {
                var clientService = await call();

                Assert.IsType<DerivedITypeImplWithConstructorParameters>(clientService);
                Assert.Same(injectedService, ((DerivedITypeImplWithConstructorParameters) clientService).InjectedService);
            });
        }
        
        private interface IGenericType<T>
        {
        
        }

        private interface IType
        {
        
        }

        private class NonDerivedITypeImpl : IType
        {
        
        }

        private class DerivedITypeImpl : ServerService, IType
        {
        
        }

        public interface IInjectedService
        {
        
        }

        private class InjectedService : IInjectedService
        {
        
        }

        private class DerivedITypeImplWithConstructorParameters : ServerService, IType
        {
            public IInjectedService InjectedService { get; private set; }

            public DerivedITypeImplWithConstructorParameters(IInjectedService injectedService)
            {
                InjectedService = injectedService;
            }
        }
    }
}