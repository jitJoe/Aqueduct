using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aqueduct.Server.Transport.SignalR
{
    public interface IAqueductHub
    {
        Task ReceiveInvocationAsync(Guid invocationId, string service, string methodName, List<string> methodParameterTypes, List<byte[]> methodArguments);
        Task ReceiveCallbackAsync(Guid invocationId, byte[] returnValue, byte[] exceptionValue);
    }
}