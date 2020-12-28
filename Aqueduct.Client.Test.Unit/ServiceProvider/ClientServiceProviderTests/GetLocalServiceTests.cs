using System;
using System.Collections.Generic;
using Xunit;

namespace Aqueduct.Client.Test.Unit.ServiceProvider.ClientServiceProviderTests
{
    public class GetLocalServiceTests : ClientServiceProviderTestsBase
    {
        private void ForBothCallStyles<TServiceType, TLocalServiceType>(Action<Func<object>> test) where TServiceType : class where TLocalServiceType : class
        {
            try
            {
                test(() => _clientServiceProvider.GetLocalService<TServiceType, TLocalServiceType>());
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with generic style", exception);
            }

            try
            {
                test(() => _clientServiceProvider.GetLocalService(typeof(TServiceType), typeof(TLocalServiceType)));
            }
            catch (Exception exception)
            {
                throw new Exception("Exception calling with Type references", exception);
            }
        }
        
        [Fact]
        public void Unbound_Generic_Type_Throws()
        {
            var exception = Assert.Throws<Exception>(() => _clientServiceProvider.GetLocalService(typeof(IGenericType<>), typeof(IGenericType<>)));
            
            Assert.Equal("Cannot create instance of generic client type", exception.Message);
        }
        
        [Fact]
        public void Bound_Generic_Type_Throws()
        {
            ForBothCallStyles<IGenericType<string>, IGenericType<string>>((call) =>
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
            
            ForBothCallStyles<IType, ILocalType>((call) =>
            {
                var exception = Assert.Throws<Exception>(call);
            
                Assert.StartsWith("Cannot find implementation for", exception.Message);
            });
        }
        
        [Fact]
        public void Implementation_Registered_With_TypeFinder_Does_Not_Derive_From_Client_Service_Throws()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(NonDerivedITypeImpl));
            
            ForBothCallStyles<IType, ILocalType>((call) =>
            {
                var exception = Assert.Throws<Exception>(call);

                Assert.EndsWith("as implementation does not derive from ClientService", exception.Message);
            });
        }
        
        [Fact]
        public void No_Constructor_Injection()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(DerivedITypeImpl));
            
            ForBothCallStyles<IType, ILocalType>((call) =>
            {
                Assert.IsType<DerivedITypeImpl>(call());
            });
        }
        
        [Fact]
        public void Constructor_Injection()
        {
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByInterfaceImplementations("Services", new List<Type> { typeof(IType), typeof(ILocalType) }))
                .Returns(typeof(DerivedITypeImplWithConstructorParameters));
            
            var injectedService = new InjectedService();
                
            _serviceProviderMock.Setup(serviceProvider => serviceProvider.GetService(typeof(IInjectedService)))
                .Returns(injectedService);
            
            ForBothCallStyles<IType, ILocalType>((call) =>
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

        private interface ILocalType
        {
            
        }

        private class NonDerivedITypeImpl : IType, ILocalType
        {
        
        }

        private class DerivedITypeImpl : ClientService, IType, ILocalType
        {
        
        }

        public interface IInjectedService
        {
        
        }

        private class InjectedService : IInjectedService
        {
            
        }

        private class DerivedITypeImplWithConstructorParameters : ClientService, IType, ILocalType
        {
            public IInjectedService InjectedService { get; private set; }

            public DerivedITypeImplWithConstructorParameters(IInjectedService injectedService)
            {
                InjectedService = injectedService;
            }
        }
    }
}