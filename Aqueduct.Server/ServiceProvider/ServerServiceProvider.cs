using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aqueduct.Server.Transport;
using Aqueduct.Server.Transport.SignalR;
using Aqueduct.Shared.Proxy;

namespace Aqueduct.Server.ServiceProvider
{
    public class ServerServiceProvider : IServerServiceProvider
    {
        private readonly ITypeFinder _typeFinder;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionIdMappingRegistry _connectionIdMappingRegistry;
        private readonly IProxyProvider _proxyProvider;
        private readonly IServerTransportDriver _serverTransportDriver;

        public ServerServiceProvider(ITypeFinder typeFinder, IServiceProvider serviceProvider, 
            IConnectionIdMappingRegistry connectionIdMappingRegistry, IProxyProvider proxyProvider, 
            IServerTransportDriver serverTransportDriver)
        {
            _typeFinder = typeFinder;
            _serviceProvider = serviceProvider;
            _connectionIdMappingRegistry = connectionIdMappingRegistry;
            _proxyProvider = proxyProvider;
            _serverTransportDriver = serverTransportDriver;
        }

        public async Task<T> GetServerServiceAsync<T>(Guid connectionId) where T : class
        {
            return (await GetServerServiceAsync(typeof(T), connectionId)) as T;
        }

        public Task<object> GetServerServiceAsync(Type serviceType, Guid connectionId) =>
            Task.FromResult(GetServerServiceInternal(connectionId, new List<Type> { serviceType }));

        public async Task<TLocalService> GetLocalServerServiceAsync<TService, TLocalService>(Guid connectionId)
            where TService : class where TLocalService : class
        {
            return (await GetLocalServerServiceAsync(typeof(TService), typeof(TLocalService), connectionId)) as TLocalService;
        }

        public Task<object> GetLocalServerServiceAsync(Type serviceType, Type serviceLocalType, Guid connectionId) =>
            Task.FromResult(GetServerServiceInternal(connectionId, new List<Type> { serviceType, serviceLocalType }));

        public async Task<List<T>> GetServerServiceForAllConnectionsAsync<T>() where T : class
        {
            return (await GetServerServiceForAllConnectionsAsync(typeof(T)))
                .Select(service => service as T).ToList();
        }

        public async Task<List<object>> GetServerServiceForAllConnectionsAsync(Type serviceType)
        {
            var connectionIds = await _connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync();

            var services = new List<object>();
            foreach (var connectionId in connectionIds)
            {
                services.Add(await GetServerServiceAsync(serviceType, connectionId));
            }

            return services;
        }

        public async Task<List<TLocalService>> GetLocalServerServiceForAllConnectionsAsync<TService, TLocalService>()
            where TService : class where TLocalService : class
        {
            return (await GetLocalServerServiceForAllConnectionsAsync(typeof(TService), typeof(TLocalService)))
                .Select(service => service as TLocalService).ToList();
        }

        public async Task<List<object>> GetLocalServerServiceForAllConnectionsAsync(Type serviceType, Type localType)
        {
            var connectionIds = await _connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync();

            var services = new List<object>();
            foreach (var connectionId in connectionIds)
            {
                services.Add(await GetLocalServerServiceAsync(serviceType, localType, connectionId));
            }

            return services;
        }

        public Task<TClientService> GetClientServiceAsync<TClientService>(Guid connectionId) where TClientService : class
        {
            var proxyType = _proxyProvider.GetProxyType<TClientService, ServerToClientInvocationMetaData>
                (_serverTransportDriver.InvocationHandler);

            var service = (TClientService) Activator.CreateInstance(proxyType, 
                _serverTransportDriver.InvocationHandler, new ServerToClientInvocationMetaData
                {
                    AqueductConnectionId = connectionId.ToString()
                });

            return Task.FromResult(service);
        }

        public async Task<List<TClientService>> GetClientServiceForAllConnectionsAsync<TClientService>() where TClientService : class
        {
            var connectionIds = await _connectionIdMappingRegistry.GetAllAqueductConnectionIdsAsync();

            var services = new List<TClientService>();
            foreach (var connectionId in connectionIds)
            {
                services.Add(await GetClientServiceAsync<TClientService>(connectionId));
            }

            return services;
        }

        private object GetServerServiceInternal(Guid connectionId, List<Type> interfaces)
        {
            if (interfaces.Any(i => i.IsGenericType))
            {
                throw new Exception("Cannot create instance of generic server type");
            }
            
            var implementationType = _typeFinder.GetTypeByInterfaceImplementations("Services", interfaces);

            if (implementationType == null)
            {
                throw new Exception($"Cannot find implementation for {string.Join(",", interfaces)}");
            }
            
            if (! implementationType.IsSubclassOf(typeof(ServerService)))
            {
                throw new Exception($"Cannot create service for {string.Join(",", interfaces)}/{implementationType} as implementation does not derive from ServerService");
            }

            var constructor = implementationType.GetConstructors().FirstOrDefault();
            var constructorArguments = constructor == null
                ? new List<object>()
                : constructor.GetParameters().Select(p => _serviceProvider.GetService(p.ParameterType));

            var implementation = (ServerService) Activator.CreateInstance(implementationType, constructorArguments.ToArray());

            implementation.ConnectionId = connectionId;
            implementation.ServerServiceProvider = this;
            implementation.ServerTransportDriver = _serverTransportDriver;

            return implementation;
        }
    }
}