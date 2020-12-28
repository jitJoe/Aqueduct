using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aqueduct.Server.ServiceProvider
{
    public interface 
        IServerServiceProvider
    {
        Task<T> GetServerServiceAsync<T>(Guid connectionId) where T : class;
        Task<object> GetServerServiceAsync(Type serviceType, Guid connectionId);
        Task<TLocalService> GetLocalServerServiceAsync<TService, TLocalService>(Guid connectionId) 
            where TService : class where TLocalService : class;
        Task<object> GetLocalServerServiceAsync(Type serviceType, Type serviceLocalType, Guid connectionId);
        Task<List<T>> GetServerServiceForAllConnectionsAsync<T>() where T : class;
        Task<List<object>> GetServerServiceForAllConnectionsAsync(Type serviceType);

        Task<List<TLocalService>> GetLocalServerServiceForAllConnectionsAsync<TService, TLocalService>()
            where TService : class where TLocalService : class;
        Task<List<object>> GetLocalServerServiceForAllConnectionsAsync(Type serviceType, Type localType);

        Task<TClientService> GetClientServiceAsync<TClientService>(Guid connectionId) where TClientService : class;

        Task<List<TClientService>> GetClientServiceForAllConnectionsAsync<TClientService>() where TClientService : class;
    }
}