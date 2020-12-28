using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aqueduct.Server.ServiceProvider;
using Aqueduct.Server.Transport;
using Aqueduct.Server.Transport.SignalR;
using Aqueduct.Shared.CallbackRegistry;
using Aqueduct.Shared.Proxy;
using Aqueduct.Shared.Serialisation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aqueduct.Server.Test.Integration.Transport.SignalR.SignalRHubTransportDriverTests
{
    public abstract class SignalRHubTransportDriverTestsBase : IDisposable
    {
        protected IHost _host;
        protected HubConnection _hubConnection;

        protected SignalRHubTransportDriverTestsBase()
        {
            
        }

        protected async Task StartServerAndClientAsync()
        {
            _host = Host.CreateDefaultBuilder(null)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:5678");
                    webBuilder.UseStartup<TestStartup>();
                })
                .Build();

            await _host.StartAsync();

            TestBridge.ServerTransportDriver = _host.Services.GetRequiredService<IServerTransportDriver>();
            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5678/aqueduct")
                .Build();

            await _hubConnection.StartAsync();
        }

        public void Dispose()
        {
            _host?.Dispose();
        }
    }

    public static class TestBridge
    {
        public static Mock<IServerServiceProvider> ServerServiceProviderMock = new();
        public static Mock<ISerialisationDriver> SerialisationDriverMock = new();
        public static Mock<ITypeFinder> TypeFinderMock = new();
        public static Mock<ICallbackRegistry> CallbackRegistryMock = new();
        public static Mock<IConnectionIdMappingRegistry> ConnectionIdMappingRegistryMock = new();
        public static Mock<ILogger<SignalRHubInboundTransportDriver>> HubLoggerMock = new();
        public static IServerTransportDriver ServerTransportDriver;
    }
    
    public class TestStartup
    {
        public IConfiguration _configuration;

        public TestStartup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IServerServiceProvider>(TestBridge.ServerServiceProviderMock.Object);
            services.AddSingleton<ISerialisationDriver>(TestBridge.SerialisationDriverMock.Object);
            services.AddSingleton<ITypeFinder>(TestBridge.TypeFinderMock.Object);
            services.AddSingleton<ICallbackRegistry>(TestBridge.CallbackRegistryMock.Object);
            services.AddSingleton<IConnectionIdMappingRegistry>(TestBridge.ConnectionIdMappingRegistryMock.Object);
            services.AddSingleton<IServerTransportDriver, SignalRHubOutboundTransportDriver>();
            services.AddSingleton<ILogger<SignalRHubInboundTransportDriver>>(TestBridge.HubLoggerMock.Object);

            services.AddSignalR(options =>
            {
                options.StreamBufferCapacity = 30;
                options.MaximumParallelInvocationsPerClient = 10;
            });
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<SignalRHubInboundTransportDriver>("/aqueduct");
            });
        }
    }
}