using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Aqueduct.Shared.DateTime;

namespace Aqueduct.Shared.CallbackRegistry
{
    public class CallbackRegistry : ICallbackRegistry
    {
        private readonly ConcurrentDictionary<Guid, Callback> _callbacks = new ConcurrentDictionary<Guid, Callback>();

        private readonly AqueductSharedConfiguration _aqueductSharedConfiguration;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CallbackRegistry(AqueductSharedConfiguration aqueductSharedConfiguration, IDateTimeProvider dateTimeProvider)
        {
            _aqueductSharedConfiguration = aqueductSharedConfiguration;
            _dateTimeProvider = dateTimeProvider;
        }

        public Task RegisterCallback(Guid invocationId, Guid? connectionId = null)
        {
            var completionSource = new TaskCompletionSource<object>();
            
            var callback = new Callback
            {
                ConnectionId = connectionId,
                ReturnType = null,
                CompletionSource = completionSource,
                ExpiresAt = _dateTimeProvider.Now() + TimeSpan.FromMilliseconds(_aqueductSharedConfiguration.CallbackTimeoutMillis),
                Expire = () => completionSource.SetCanceled()
            };

            if (! _callbacks.TryAdd(invocationId, callback))
            {
                throw new Exception("Could not register callback");
            }

            return completionSource.Task;
        }

        public Task<T> RegisterValuedCallback<T>(Guid invocationId, Guid? connectionId = null)
        {
            var completionSource = new TaskCompletionSource<T>();

            var callback = new Callback
            {
                ConnectionId = connectionId,
                ReturnType = typeof(T),
                CompletionSource = completionSource,
                ExpiresAt = _dateTimeProvider.Now() + TimeSpan.FromMilliseconds(_aqueductSharedConfiguration.CallbackTimeoutMillis),
                Expire = () => completionSource.SetCanceled()
            };

            if (!_callbacks.TryAdd(invocationId, callback))
            {
                throw new Exception("Could not register valued callback");
            }

            return completionSource.Task;
        }

        public void PerformValuedCallback<T>(Guid invocationId, T result, Guid? connectionId = null)
        {
            if (! _callbacks.TryGetValue(invocationId, out var callback))
            {
                throw new Exception("Could not get callback");
            }

            var completionSource = callback.CompletionSource as TaskCompletionSource<T>;

            if (completionSource == null || completionSource.Task.IsCanceled || completionSource.Task.IsCanceled ||
                completionSource.Task.IsCompleted)
            {
                throw new Exception("Attempted to perform valued callback but completion source Task has already been used");
            }
            
            completionSource.SetResult(result);
            
            _callbacks.TryRemove(invocationId, out _);
        }

        public void PerformCallback(Guid invocationId, Guid? connectionId = null) => PerformValuedCallback<object>(invocationId, null, connectionId);

        public void ThrowForCallback(Guid invocationId, Exception exception, Guid? connectionId = null) =>
            ThrowForValuedCallback<object>(invocationId, exception, connectionId);

        public void ThrowForValuedCallback<T>(Guid invocationId, Exception exception, Guid? connectionId = null)
        {
            if (! _callbacks.TryGetValue(invocationId, out var callback))
            {
                throw new Exception("Could not get callback");
            }

            var completionSource = callback.CompletionSource as TaskCompletionSource<T>;

            if (completionSource == null || completionSource.Task.IsCanceled || completionSource.Task.IsCanceled ||
                completionSource.Task.IsCompleted)
            {
                throw new Exception("Attempted to throw for valued callback but completion source Task has already been used");
            }
            
            completionSource.SetException(exception);
            
            _callbacks.TryRemove(invocationId, out _);
        }

        public Type GetCallbackReturnType(Guid invocationId)
        {
            if (_callbacks.TryGetValue(invocationId, out var callback))
            {
                return callback.ReturnType;
            }
            
            throw new Exception("Could not get callback return type");
        }

        public void ClearExpiredCallbacks()
        {
            foreach (var callback in _callbacks)
            {
                if (callback.Value.ExpiresAt < _dateTimeProvider.Now())
                {
                    callback.Value.Expire();
                    _callbacks.TryRemove(callback.Key, out _);
                }
            }
        }

        private class Callback
        {
            internal Guid? ConnectionId { get; set; }
            internal Type ReturnType { get; set; }
            internal object CompletionSource { get; set; }
            internal DateTimeOffset ExpiresAt { get; set; }
            internal Action Expire { get; set; }
        }
    }
}