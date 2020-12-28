using System;
using System.Collections.Generic;
using Xunit;

namespace Aqueduct.Client.Test.Unit.ServiceProvider.ClientServiceProviderTests
{
    public class GetClientServiceTests : ClientServiceProviderTestsBase
    {
        private void ForBothCallStyles<T>(Action<Func<object>> test) where T : class
        {
            try
            {
                test(() => _clientServiceProvider.GetClientService<T>());
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with generic style", exception);
            }

            try
            {
                test(() => _clientServiceProvider.GetClientService(typeof(T)));
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with Type reference", exception);
            }
        }

        [Fact]
        public void Unbound_Generic_Type_Throws()
        {
            var exception = Assert.Throws<Exception>(() => _clientServiceProvider.GetClientService(typeof(IGenericType<>)));
            
            Assert.Equal("Cannot create instance of generic client type", exception.Message);
        }
        
        [Fact]
        public void Bound_Generic_Type_Throws()
        {
            ForBothCallStyles<IGenericType<string>>((call) =>
            {
                var exception = Assert.Throws<Exception>(call);
            
                Assert.Equal("Cannot create instance of generic client type", exception.Message);
            });
        }
        
        [Fact]
        public void No_Implementation_Registered_With_TypeFinder_Throws()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns((Type) null);
            
            ForBothCallStyles<IType>((call) =>
            {
                var exception = Assert.Throws<Exception>(call);
            
                Assert.StartsWith("Cannot find implementation for", exception.Message);
            });
        }
        
        [Fact]
        public void Implementation_Registered_With_TypeFinder_Does_Not_Derive_From_Client_Service_Throws()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns(typeof(NonDerivedITypeImpl));
            
            ForBothCallStyles<IType>((call) =>
            {
                var exception = Assert.Throws<Exception>(call);
            
                Assert.EndsWith("as implementation does not derive from ClientService", exception.Message);
            });
        }
        
        [Fact]
        public void No_Constructor_Injection()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns(typeof(DerivedITypeImpl));
            
            ForBothCallStyles<IType>((call) =>
            {
                Assert.IsType<DerivedITypeImpl>(call());
            });
        }
        
        [Fact]
        public void Constructor_Injection()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType) }))
                .Returns(typeof(DerivedITypeImplWithConstructorParameters));

            var injectedService = new InjectedService();
                
            _serviceProviderMock.Setup(serviceProvider => serviceProvider.GetService(typeof(IInjectedService)))
                .Returns(injectedService);

            
            ForBothCallStyles<IType>((call) =>
            {
                var clientService = call();

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

        private class DerivedITypeImpl : ClientService, IType
        {
        
        }

        public interface IInjectedService
        {
        
        }

        private class InjectedService : IInjectedService
        {
        
        }

        private class DerivedITypeImplWithConstructorParameters : ClientService, IType
        {
            public IInjectedService InjectedService { get; private set; }

            public DerivedITypeImplWithConstructorParameters(IInjectedService injectedService)
            {
                InjectedService = injectedService;
            }
        }
    }
}