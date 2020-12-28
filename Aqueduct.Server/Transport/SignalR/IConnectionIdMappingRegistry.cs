using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Aqueduct.Server.Transport.SignalR
{
    public interface IConnectionIdMappingRegistry
    {
        Task<Guid> GetAqueductConnectionIdForSignalRConnectionIdAsync(string signalRConnectionId);
        Task<string> GetSignalRConnectionIdForAqueductConnectionIdAsync(Guid aqueductConnectionId);
        Task<ImmutableList<Guid>> GetAllAqueductConnectionIdsAsync();
        Task RemoveConnectionAsync(Guid aqueductConnectionId);
    }
}