using System;

namespace Aqueduct.Client.ServiceProvider
{
    public interface IClientServiceProvider
    {
        TClientService GetClientService<TClientService>() where TClientService : class;
        object GetClientService(Type serviceType);
        TClientLocalService GetLocalService<TClientService, TClientLocalService>() 
            where TClientService : class where TClientLocalService : class;

        object GetLocalService(Type serviceType, Type serviceLocalType);

        TServerService GetServerService<TServerService>() where TServerService : class;
    }
}