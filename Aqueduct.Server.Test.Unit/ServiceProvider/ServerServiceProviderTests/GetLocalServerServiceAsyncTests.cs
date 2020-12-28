using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Aqueduct.Server.Test.Unit.ServiceProvider.ServerServiceProviderTests
{
    public class GetLocalServerServiceAsyncTests : ServerServiceProviderTestsBase
    {
        private async Task ForBothCallStylesAsync<TService, TLocalService>(Guid connectionId, Func<Func<Task<object>>, Task> test) 
            where TService : class where TLocalService : class
        {
            try
            {
                await test(async () => await _serverServiceProvider.GetLocalServerServiceAsync<TService, TLocalService>(connectionId));
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with generic style", exception);
            }

            try
            {
                await test(async () => await _serverServiceProvider.GetLocalServerServiceAsync(typeof(TService), typeof(TLocalService), connectionId));
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with Type reference", exception);
            }
        }
        
        [Fact]
        public async Task Unbound_Generic_Type_Throws()
        {
            var exception = await Assert.ThrowsAsync<Exception>(() => 
                _serverServiceProvider.GetLocalServerServiceAsync(typeof(IGenericType<>), typeof(IGenericType<>), Guid.NewGuid()));
            
            Assert.Equal("Cannot create instance of generic server type", exception.Message);
        }
        
        [Fact]
        public async Task Bound_Generic_Type_Throws()
        {
            await ForBothCallStylesAsync<IGenericType<string>, IGenericType<string>>(Guid.NewGuid(), async (call) =>
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
            
            await ForBothCallStylesAsync<IType, ILocalType>(Guid.NewGuid(), async (call) =>
            {
                var exception = await Assert.ThrowsAsync<Exception>(call);
            
                Assert.StartsWith("Cannot find implementation for", exception.Message);
            });
        }
        
        [Fact]
        public async Task Implementation_Registered_With_TypeFinder_Does_Not_Derive_From_Server_Service_Throws()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(NonDerivedITypeImpl));
            
            await ForBothCallStylesAsync<IType, ILocalType>(Guid.NewGuid(), async (call) =>
            {
                var exception = await Assert.ThrowsAsync<Exception>(call);

                Assert.EndsWith("as implementation does not derive from ServerService", exception.Message);
            });
        }
        
        [Fact]
        public async Task No_Constructor_Injection()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(DerivedITypeImpl));
            
            await ForBothCallStylesAsync<IType, ILocalType>(Guid.NewGuid(), async (call) =>
            {
                Assert.IsType<DerivedITypeImpl>(await call());
            });
        }
        
        [Fact]
        public async Task Constructor_Injection()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(DerivedITypeImplWithConstructorParameters));
            
            var injectedService = new InjectedService();
                
            _serviceProviderMock.Setup(serviceProvider => serviceProvider.GetService(typeof(IInjectedService)))
                .Returns(injectedService);
            
            await ForBothCallStylesAsync<IType, ILocalType>(Guid.NewGuid(), async (call) =>
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