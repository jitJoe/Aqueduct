using System;
using Aqueduct.Server.ServiceProvider;
using Aqueduct.Server.Transport;

namespace Aqueduct.Server
{
    public abstract class ServerService
    {
        public Guid ConnectionId { get; internal set; }
        public IServerServiceProvider ServerServiceProvider { get; internal set; }
        public IServerTransportDriver ServerTransportDriver { get; set; }
    }
}