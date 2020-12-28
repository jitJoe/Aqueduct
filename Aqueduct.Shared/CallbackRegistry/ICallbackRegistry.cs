using System;
using System.Threading.Tasks;

namespace Aqueduct.Shared.CallbackRegistry
{
    public interface ICallbackRegistry
    {
        Task RegisterCallback(Guid invocationId, Guid? connectionId = null);
        Task<T> RegisterValuedCallback<T>(Guid invocationId, Guid? connectionId = null);
        void PerformValuedCallback<T>(Guid invocationId, T result, Guid? connectionId = null);
        void PerformCallback(Guid invocationId, Guid? connectionId = null);
        void ThrowForCallback(Guid invocationId, Exception exception, Guid? connectionId = null);
        void ThrowForValuedCallback<T>(Guid invocationId, Exception exception, Guid? connectionId = null);
        Type GetCallbackReturnType(Guid invocationId);
        void ClearExpiredCallbacks();
    }
}