using System;
using Aqueduct.Server.Cleanup;
using Aqueduct.Server.ServiceProvider;
using Aqueduct.Server.Transport;
using Aqueduct.Server.Transport.SignalR;
using Aqueduct.Shared;
using Aqueduct.Shared.CallbackRegistry;
using Aqueduct.Shared.DateTime;
using Aqueduct.Shared.Proxy;
using Aqueduct.Shared.Serialisation;
using Microsoft.Extensions.DependencyInjection;

namespace Aqueduct.Server.Extensions
{
    public static class AddAqueductExtensions
    {
        public static void AddAqueduct(this IServiceCollection services, Action<AqueductServerConfiguration> configure)
        {
            var serverConfiguration = new AqueductServerConfiguration();

            configure(serverConfiguration);
            
            services.AddSingleton<AqueductSharedConfiguration>(new AqueductSharedConfiguration
            {
                CallbackTimeoutMillis = serverConfiguration.CallbackTimeoutMillis
            });
            
            services.AddSingleton<AqueductSharedConfiguration>(new AqueductSharedConfiguration());
            
            var typeFinder = new TypeFinder();
            typeFinder.RegisterTypeList("Serialisable", serverConfiguration.SerialisableTypeList);
            typeFinder.RegisterTypeList("Services", serverConfiguration.ServicesTypeList);

            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddSingleton<ITypeFinder>(typeFinder);
            services.AddSingleton<IProxyProvider, ProxyProvider>();
            services.AddSingleton<IConnectionIdMappingRegistry, ConnectionIdMappingRegistry>();
            services.AddSingleton<IServerServiceProvider, ServerServiceProvider>();
            services.AddSingleton<ISerialisationDriver, JsonNetSerialisationDriver>();
            services.AddSingleton<ICallbackRegistry, CallbackRegistry>();
            services.AddSingleton<IServerTransportDriver, SignalRHubOutboundTransportDriver>();

            services.AddHostedService<CleanUpHostedService>();
        }
    }
}