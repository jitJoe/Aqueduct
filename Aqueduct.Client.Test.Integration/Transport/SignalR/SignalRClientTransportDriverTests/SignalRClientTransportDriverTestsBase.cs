using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aqueduct.Client.ServiceProvider;
using Aqueduct.Client.Transport.SignalR;
using Aqueduct.Shared.CallbackRegistry;
using Aqueduct.Shared.Proxy;
using Aqueduct.Shared.Serialisation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aqueduct.Client.Test.Integration.Transport.SignalR.SignalRClientTransportDriverTests
{
    public abstract class SignalRClientTransportDriverTestsBase : IDisposable
    {
        protected TestHubAccessor _testHubAccessor;

        protected IHost _host;
        
        protected readonly NavigationManagerMock _navigationManagerMock = new NavigationManagerMock();
        protected readonly Mock<ISerialisationDriver> _serialisationDriverMock = new Mock<ISerialisationDriver>();
        protected readonly Mock<ICallbackRegistry> _callbackRegistryMock = new Mock<ICallbackRegistry>();
        protected readonly Mock<ITypeFinder> _typeFinderMock = new Mock<ITypeFinder>();
        protected readonly Mock<IServiceProvider> _serviceProviderMock = new Mock<IServiceProvider>();
        protected readonly Mock<IClientServiceProvider> _clientServiceProviderMock = new Mock<IClientServiceProvider>();
        protected readonly Mock<ILogger<SignalRClientTransportDriver>> _loggerMock = new Mock<ILogger<SignalRClientTransportDriver>>(MockBehavior.Loose);

        protected readonly SignalRClientTransportDriver _signalRClientTransportDriver;

        protected SignalRClientTransportDriverTestsBase()
        {
            _signalRClientTransportDriver = new SignalRClientTransportDriver(_navigationManagerMock,
                _serialisationDriverMock.Object,
                _callbackRegistryMock.Object,
                _typeFinderMock.Object,
                _serviceProviderMock.Object,
                _loggerMock.Object);

            _serviceProviderMock.Setup(serviceProvider => serviceProvider.GetService(typeof(IClientServiceProvider)))
                .Returns(_clientServiceProviderMock.Object);
        }

        protected void StartServer()
        {
            _host = Host.CreateDefaultBuilder(null)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:5678");
                    webBuilder.UseStartup<TestStartup>();
                })
                .Build();
            
            _host.StartAsync();

            _testHubAccessor = _host.Services.GetRequiredService<TestHubAccessor>();
            _testHubAccessor.HubContext = _host.Services.GetRequiredService<IHubContext<TestHub>>();
        }

        public void Dispose()
        {
            _host?.Dispose();
        }
    }
    
    public class NavigationManagerMock : NavigationManager
    {
        public NavigationManagerMock()
        {
            Initialize("http://localhost:5678/", "http://localhost:5678");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            
        }
    }

    public class TestHubAccessor
    {
        public int ConnectedCount { get; set; } = 0;
        public IHubContext<TestHub> HubContext { get; set; }
        public List<CallbackInvocation> CallbackInvocations { get; } = new();
        public Func<Guid, string, string, List<string>, List<byte[]>, Task> OnReceiveInvocationAsync { get; set; }
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
            services.AddSingleton<TestHubAccessor>();
            
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
                endpoints.MapHub<TestHub>("/aqueduct");
            });
        }
    }
    
    public class TestHub : Hub
    {
        private readonly TestHubAccessor _testHubAccessor;

        public TestHub(TestHubAccessor testHubAccessor)
        {
            _testHubAccessor = testHubAccessor;
        }

        public override Task OnConnectedAsync()
        {
            _testHubAccessor.ConnectedCount += 1;

            return base.OnConnectedAsync();
        }

        public async Task ReceiveInvocationAsync(Guid invocationId, string service, string methodName, List<string> methodParameterTypes, List<byte[]> methodArguments)
        {
            if (_testHubAccessor.OnReceiveInvocationAsync != null)
            {
                await _testHubAccessor.OnReceiveInvocationAsync(invocationId, service, methodName, methodParameterTypes, methodArguments);
            }
        }
        
        public Task ReceiveCallbackAsync(Guid invocationId, byte[] returnValue, byte[] exceptionValue)
        {
            _testHubAccessor.CallbackInvocations.Add(new CallbackInvocation
            {
                InvocationId = invocationId,
                ReturnValue = returnValue,
                ExceptionValue = exceptionValue
            });

            return Task.CompletedTask;
        }
    }
    
    public class CallbackInvocation
    {
        public Guid InvocationId {get; set; }
        public byte[] ReturnValue { get; set; }
        public byte[] ExceptionValue { get; set; }
    }
}