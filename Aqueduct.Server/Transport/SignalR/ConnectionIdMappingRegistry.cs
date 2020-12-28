using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Aqueduct.Server.Transport.SignalR
{
    public class ConnectionIdMappingRegistry : IConnectionIdMappingRegistry
    {
        private readonly ConcurrentDictionary<string, Guid> _signalRToAqueduct = new ConcurrentDictionary<string, Guid>();
        private readonly ConcurrentDictionary<Guid, string> _aqueductToSignalR = new ConcurrentDictionary<Guid, string>();
        
        public Task<Guid> GetAqueductConnectionIdForSignalRConnectionIdAsync(string signalRConnectionId)
        {
            if (_signalRToAqueduct.TryGetValue(signalRConnectionId, out var aqueductId))
            {
                return Task.FromResult(aqueductId);
            }

            var newAqueductId = Guid.NewGuid();

            if (!_signalRToAqueduct.TryAdd(signalRConnectionId, newAqueductId) || !_aqueductToSignalR.TryAdd(newAqueductId, signalRConnectionId))
            {
                throw new Exception("Could not create SignalR<->Aqueduct ID mapping");
            }

            return Task.FromResult(newAqueductId);
        }

        public Task<string> GetSignalRConnectionIdForAqueductConnectionIdAsync(Guid aqueductConnectionId)
        {
            if (_aqueductToSignalR.TryGetValue(aqueductConnectionId, out var signalRId))
            {
                return Task.FromResult(signalRId);
            }

            return Task.FromResult<string>(null);
        }

        public Task<ImmutableList<Guid>> GetAllAqueductConnectionIdsAsync()
        {
            return Task.FromResult(_signalRToAqueduct.Values.ToImmutableList());
        }

        public async Task RemoveConnectionAsync(Guid aqueductConnectionId)
        {
            var signalR = await GetSignalRConnectionIdForAqueductConnectionIdAsync(aqueductConnectionId);
            _aqueductToSignalR.Remove(aqueductConnectionId, out _);
            _signalRToAqueduct.Remove(signalR, out _);
        }
    }
}