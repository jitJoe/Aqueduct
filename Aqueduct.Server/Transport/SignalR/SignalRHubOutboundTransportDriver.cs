using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aqueduct.Shared.CallbackRegistry;
using Aqueduct.Shared.Proxy;
using Aqueduct.Shared.Serialisation;
using Microsoft.AspNetCore.SignalR;

namespace Aqueduct.Server.Transport.SignalR
{
    public class SignalRHubOutboundTransportDriver : IServerTransportDriver
    {
        public ProxyInvocationHandler<ServerToClientInvocationMetaData> InvocationHandler { get; }

        private readonly IConnectionIdMappingRegistry _connectionIdMappingRegistry;
        private readonly ISerialisationDriver _serialisationDriver;
        private readonly ICallbackRegistry _callbackRegistry;
        private readonly IHubContext<SignalRHubInboundTransportDriver, IAqueductHub> _hubContext;
        
        private readonly MethodInfo _callbackRegistryRegisterValuedCallbackMethod =
            typeof(ICallbackRegistry).GetMethod("RegisterValuedCallback");

        public SignalRHubOutboundTransportDriver(IConnectionIdMappingRegistry connectionIdMappingRegistry, ISerialisationDriver serialisationDriver, 
            ICallbackRegistry callbackRegistry, IHubContext<SignalRHubInboundTransportDriver, IAqueductHub> hubContext)
        {
            InvocationHandler = new ProxyInvocationHandler<ServerToClientInvocationMetaData>(InvokeAsync);
            
            _connectionIdMappingRegistry = connectionIdMappingRegistry;
            _serialisationDriver = serialisationDriver;
            _callbackRegistry = callbackRegistry;
            _hubContext = hubContext;
        }

        private object InvokeAsync(MethodInfo methodInfo, object[] arguments, ServerToClientInvocationMetaData metaData)
        {
            var signalRConnectionId = _connectionIdMappingRegistry.
                GetSignalRConnectionIdForAqueductConnectionIdAsync(Guid.Parse(metaData.AqueductConnectionId)).Result;
            
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
                    .Invoke(_callbackRegistry, new object[] { invocationId, Guid.Parse(metaData.AqueductConnectionId) });
            }
            
            //TODO: We should await this rather than blocking but would require generating some more complicated IL in the proxy
            _hubContext.Clients.Client(signalRConnectionId).ReceiveInvocationAsync(invocationId, methodInfo.DeclaringType.AssemblyQualifiedName, 
                methodInfo.Name, parameterTypes, serialisedArguments).Wait();

            return returnTask;
        }
    }
}