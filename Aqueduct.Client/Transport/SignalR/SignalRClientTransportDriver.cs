using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Aqueduct.Client.ServiceProvider;
using Aqueduct.Shared.CallbackRegistry;
using Aqueduct.Shared.Extensions;
using Aqueduct.Shared.Proxy;
using Aqueduct.Shared.Serialisation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aqueduct.Client.Transport.SignalR
{
    public class SignalRClientTransportDriver : IClientTransportDriver
    {
        public ProxyInvocationHandler<ClientToServerInvocationMetaData> InvocationHandler { get; }

        private readonly NavigationManager _navigationManager;
        private readonly ISerialisationDriver _serialisationDriver;
        private readonly ICallbackRegistry _callbackRegistry;
        private readonly ITypeFinder _typeFinder;
        private IClientServiceProvider _clientServiceProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SignalRClientTransportDriver> _logger;
        private HubConnection _hubConnection;
        private Func<Task> _onDisconnected;
        private Timer _connectionCheckTimer;

        private readonly MethodInfo _callbackRegistryPerformValuedCallbackMethod = 
            typeof(ICallbackRegistry).GetMethod("PerformValuedCallback");

        private readonly MethodInfo _callbackRegistryRegisterValuedCallbackMethod =
            typeof(ICallbackRegistry).GetMethod("RegisterValuedCallback");

        private readonly MethodInfo _callbackRegistryThrowForValuedCallbackMethod =
            typeof(ICallbackRegistry).GetMethod("ThrowForValuedCallback");
        
        public SignalRClientTransportDriver(NavigationManager navigationManager, ISerialisationDriver serialisationDriver,
            ICallbackRegistry callbackRegistry, ITypeFinder typeFinder, IServiceProvider serviceProvider, ILogger<SignalRClientTransportDriver> logger)
        {
            InvocationHandler = new ProxyInvocationHandler<ClientToServerInvocationMetaData>(InvokeAsync);

            _navigationManager = navigationManager;
            _serialisationDriver = serialisationDriver;
            _callbackRegistry = callbackRegistry;
            _typeFinder = typeFinder;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                _logger.LogWarning("SignalRClientTransportDriver is already connected");
                return;
            }
            
            _logger.LogInformation("Starting SignalRClientTransportDriver...");

            if (_hubConnection == null)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_navigationManager.ToAbsoluteUri("/aqueduct"))
                    .Build();

                _hubConnection.On<Guid, string, string, List<string>, List<byte[]>>("ReceiveInvocationAsync",
                    ReceiveInvocationAsync);
                _hubConnection.On<Guid, byte[], byte[]>("ReceiveCallbackAsync", ReceiveCallbackAsync);
            }

            try
            {
                await _hubConnection.StartAsync();
                _connectionCheckTimer = new Timer(500);
                _connectionCheckTimer.Elapsed += (_, _) =>
                {
                    if (_hubConnection == null || _hubConnection.State == HubConnectionState.Disconnected)
                    {
                        _connectionCheckTimer.Stop();
                        _onDisconnected?.Invoke();
                    }
                };
                _connectionCheckTimer.Start();
            }
            catch (Exception exception)
            {
                throw new Exception("Cannot connect to Server", exception);
            }

            _logger.LogInformation("Started SignalRClientTransportDriver...");
        }

        public void OnDisconnected(Func<Task> handler)
        {
            _onDisconnected = handler;
        }

        //Handles inbound invocations
        private async Task ReceiveInvocationAsync(Guid invocationId, string service, string methodName, List<string> methodParameterTypes, List<byte[]> methodArguments)
        {
            object serviceInstance;
            Type[] resolvedParameterTypes;
            object[] deserialisedMethodArguments;
            Type returnType;
            try
            {
                var serviceType = _typeFinder.GetTypeByName("Services", service);
                serviceInstance = GetClientServiceProvider().GetClientService(serviceType);

                resolvedParameterTypes = methodParameterTypes
                    .Select(pt => _typeFinder.GetTypeByName("Serialisable", pt)).ToArray();
                returnType = serviceInstance.GetMethodReturnType(methodName, resolvedParameterTypes);

                if (returnType != typeof(Task) && !returnType.IsSubclassOf(typeof(Task)))
                {
                    throw new Exception(
                        $"Service '{service}' method '{methodName}' has non-Task return type - cannot be invoked");
                }

                deserialisedMethodArguments =
                    methodArguments.Select((a, index) => _serialisationDriver.Deserialise(a, resolvedParameterTypes[index])).ToArray();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception whilst receiving invocation");
                return;
            }

            try
            {
                var resultTask = serviceInstance.InvokeMethod(methodName, resolvedParameterTypes, deserialisedMethodArguments);

                await (Task) resultTask;
                if (returnType == typeof(Task))
                {
                    await _hubConnection.SendAsync("ReceiveCallbackAsync", invocationId, null, null);

                    return;
                }

                if (returnType.IsSubclassOf(typeof(Task)))
                {
                    var result = returnType.GetProperty("Result")?.GetValue(resultTask);

                    var serialisedResult = _serialisationDriver.Serialise(result);

                    await _hubConnection.SendAsync("ReceiveCallbackAsync", invocationId, serialisedResult, null);
                }
            }
            catch (Exception exception)
            {
                var serialisedException = _serialisationDriver.SerialiseException(exception);
                await _hubConnection.SendAsync("ReceiveCallbackAsync",invocationId, (byte[]) null, serialisedException);
            }
        }
        
        //Handles inbound callbacks
        private async Task ReceiveCallbackAsync(Guid invocationId, byte[] returnValue, byte[] exceptionValue)
        {
            try
            {
                var callbackReturnType = _callbackRegistry.GetCallbackReturnType(invocationId);

                if (returnValue != null && exceptionValue == null)
                {
                    var deserialisedReturnValue = _serialisationDriver.Deserialise(returnValue, callbackReturnType);

                    _callbackRegistryPerformValuedCallbackMethod
                        .MakeGenericMethod(callbackReturnType)
                        .Invoke(_callbackRegistry, new[] {invocationId, deserialisedReturnValue, null});

                    return;
                }

                if (returnValue == null && exceptionValue == null)
                {
                    if (callbackReturnType != null)
                    {
                        throw new Exception(
                            $"Received non-valued callback for valued callback of type {callbackReturnType}");
                    }

                    _callbackRegistry.PerformCallback(invocationId);

                    return;
                }

                var deserialisedException = _serialisationDriver.DeserialiseException(exceptionValue);

                if (callbackReturnType == null)
                {
                    _callbackRegistry.ThrowForCallback(invocationId, deserialisedException);
                }
                else
                {
                    _callbackRegistryThrowForValuedCallbackMethod
                        .MakeGenericMethod(callbackReturnType)
                        .Invoke(_callbackRegistry, new object[] { invocationId, deserialisedException, null });
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception whilst receiving callback");
            }
        }

        //Handles outbound invocations
        private object InvokeAsync(MethodInfo methodInfo, object[] arguments, ClientToServerInvocationMetaData metaData)
        {
            var invocationId = Guid.NewGuid();
            
            var parameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToList();
            var serialisedArguments = arguments.Select(a => _serialisationDriver.Serialise(a)).ToList();

            Task returnTask;
            if (methodInfo.ReturnType == typeof(Task))
            {
                returnTask = _callbackRegistry.RegisterCallback(invocationId);
            }
            else
            {
                returnTask = (Task) _callbackRegistryRegisterValuedCallbackMethod
                    .MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments())
                    .Invoke(_callbackRegistry, new object[] { invocationId, null });
            }
            
            //TODO: We should await this rather than blocking but would require generating some more complicated IL in the proxy
            _hubConnection.SendAsync("ReceiveInvocationAsync", invocationId, methodInfo!.DeclaringType!.AssemblyQualifiedName, 
                methodInfo.Name, parameterTypes, serialisedArguments);

            return returnTask;
        }

        private IClientServiceProvider GetClientServiceProvider()
        {
            if (_clientServiceProvider != null)
            {
                return _clientServiceProvider;
            }

            return _clientServiceProvider = _serviceProvider.GetRequiredService<IClientServiceProvider>();
        }
    }
}