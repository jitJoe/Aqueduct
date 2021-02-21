using System;
using System.Linq;
using System.Collections.Generic;
using Aqueduct.Client.Transport;
using Aqueduct.Shared.Proxy;
using Microsoft.Extensions.DependencyInjection;

namespace Aqueduct.Client.ServiceProvider
{
    public class ClientServiceProvider : IClientServiceProvider
    {
        private readonly ITypeFinder _typeFinder;
        private readonly IProxyProvider _proxyProvider;
        private readonly IClientTransportDriver _clientTransportDriver;
        private readonly IServiceProvider _serviceProvider;
        
        private readonly Dictionary<Type, object> _serverServices = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _clientServices = new Dictionary<Type, object>();

        public ClientServiceProvider(ITypeFinder typeFinder, IProxyProvider proxyProvider, 
            IClientTransportDriver clientTransportDriver, IServiceProvider serviceProvider)
        {
            _typeFinder = typeFinder;
            _proxyProvider = proxyProvider;
            _clientTransportDriver = clientTransportDriver;
            _serviceProvider = serviceProvider;
        }

        public TClientService GetClientService<TClientService>() where TClientService : class => 
            GetClientServiceInternal(new List<Type> { typeof(TClientService) }) as TClientService;

        public object GetClientService(Type serviceType) => GetClientServiceInternal(new List<Type> {serviceType});

        public TClientLocalService GetLocalService<TClientService, TClientLocalService>() where TClientService : class where TClientLocalService : class =>
            GetClientServiceInternal(new List<Type> { typeof(TClientService), typeof(TClientLocalService) }) as TClientLocalService;

        public object GetLocalService(Type serviceType, Type serviceLocalType) => GetClientServiceInternal(new List<Type> {serviceType, serviceLocalType});

        public TServerService GetServerService<TServerService>() where TServerService : class
        {
            if (typeof(TServerService).IsGenericType)
            {
                throw new Exception("Cannot get generic server service proxy");
            }
            
            var serverServiceType = typeof(TServerService);
            
            if (_serverServices.ContainsKey(serverServiceType))
            {
                return _serverServices[serverServiceType] as TServerService;
            }

            var proxyType = _proxyProvider.GetProxyType<TServerService, ClientToServerInvocationMetaData>
                (_clientTransportDriver.InvocationHandler);

            var service = (TServerService) Activator.CreateInstance(proxyType, _clientTransportDriver.InvocationHandler, new ClientToServerInvocationMetaData());

            _serverServices.Add(serverServiceType, service);
            
            return service;
        }
        
        private object GetClientServiceInternal(List<Type> interfaces)
        {
            if (interfaces.Any(i => i.IsGenericType))
            {
                throw new Exception("Cannot create instance of generic client type");
            }
            
            var implementationType = _typeFinder.GetTypeByInterfaceImplementations("Services", interfaces);

            if (implementationType == null)
            {
                throw new Exception($"Cannot find implementation for {string.Join(",", interfaces)}");
            }

            if (_clientServices.ContainsKey(implementationType))
            {
                return _clientServices[implementationType];
            }
            
            if (! implementationType.IsSubclassOf(typeof(ClientService)))
            {
                throw new Exception($"Cannot create service for {string.Join(",", interfaces)}/{implementationType} as implementation does not derive from ClientService");
            }

            var scope = _serviceProvider.CreateScope();
            var constructor = implementationType.GetConstructors().FirstOrDefault();
            var constructorArguments = constructor == null
                ? new List<object>()
                : constructor.GetParameters().Select(p => scope.ServiceProvider.GetService(p.ParameterType));

            var implementation = (ClientService) Activator.CreateInstance(implementationType, constructorArguments.ToArray());
            
            implementation.ClientServiceProvider = this; 

            _clientServices.Add(implementationType, implementation);
            
            return implementation;
        }
    }
}