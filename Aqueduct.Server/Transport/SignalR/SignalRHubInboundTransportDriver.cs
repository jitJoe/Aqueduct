using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aqueduct.Server.ServiceProvider;
using Aqueduct.Shared.CallbackRegistry;
using Aqueduct.Shared.Extensions;
using Aqueduct.Shared.Proxy;
using Aqueduct.Shared.Serialisation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aqueduct.Server.Transport.SignalR
{
    public class SignalRHubInboundTransportDriver : Hub<IAqueductHub>, IAqueductHub
    {
        private readonly IServerServiceProvider _serverServiceProvider;
        private readonly ITypeFinder _typeFinder;
        private readonly ISerialisationDriver _serialisationDriver;
        private readonly ICallbackRegistry _callbackRegistry;
        private readonly IConnectionIdMappingRegistry _connectionIdMappingRegistry;
        private readonly ILogger<SignalRHubInboundTransportDriver> _logger;
        
        private readonly MethodInfo _callbackRegistryPerformValuedCallbackMethod = 
            typeof(ICallbackRegistry).GetMethod("PerformValuedCallback");

        public SignalRHubInboundTransportDriver(IServerServiceProvider serverServiceProvider, ISerialisationDriver serialisationDriver, 
            ITypeFinder typeFinder, ICallbackRegistry callbackRegistry, IConnectionIdMappingRegistry connectionIdMappingRegistry, 
            ILogger<SignalRHubInboundTransportDriver> logger)
        {
            _serverServiceProvider = serverServiceProvider;
            _typeFinder = typeFinder;
            _serialisationDriver = serialisationDriver;
            _callbackRegistry = callbackRegistry;
            _connectionIdMappingRegistry = connectionIdMappingRegistry;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await _connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var aqueductId =
                await _connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(Context.ConnectionId);
            await _connectionIdMappingRegistry.RemoveConnectionAsync(aqueductId);
            
            await base.OnDisconnectedAsync(exception);
        }

        //Handles inbound invocations
        public async Task ReceiveInvocationAsync(Guid invocationId, string service, string methodName, 
            List<string> methodParameterTypes, List<byte[]> methodArguments)
        {
            Guid connectionId;
            Type serviceType;
            object serviceInstance;
            Type returnType;
            Type[] resolvedParameterTypes;
            object[] deserialisedMethodArguments;
            try
            {
                connectionId =
                    await _connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(
                        Context.ConnectionId);
                serviceType = _typeFinder.GetTypeByName("Services", service);
                serviceInstance = await _serverServiceProvider.GetServerServiceAsync(serviceType, connectionId);

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
                    await Clients.Caller.ReceiveCallbackAsync(invocationId, null, null);

                    return;
                }

                if (returnType.IsSubclassOf(typeof(Task)))
                {
                    var result = returnType.GetProperty("Result")?.GetValue(resultTask);

                    var serialisedResult = _serialisationDriver.Serialise(result);

                    await Clients.Caller.ReceiveCallbackAsync(invocationId, serialisedResult, null);
                }
            }
            catch (Exception exception)
            {
                var serialisedException = _serialisationDriver.SerialiseException(exception);
                await Clients.Caller.ReceiveCallbackAsync(invocationId, null, serialisedException);
            }
        }

        //Handles inbound callbacks
        public async Task ReceiveCallbackAsync(Guid invocationId, byte[] returnValue, byte[] exceptionValue)
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

                var deserialisedException = _serialisationDriver.Deserialise(exceptionValue);

                _callbackRegistry.ThrowForCallback(invocationId, (Exception) deserialisedException);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception whilst receiving callback");
            }
        }
    }
}