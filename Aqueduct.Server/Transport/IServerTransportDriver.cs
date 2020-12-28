using Aqueduct.Shared.Proxy;

namespace Aqueduct.Server.Transport
{
    public interface IServerTransportDriver
    {
        ProxyInvocationHandler<ServerToClientInvocationMetaData> InvocationHandler { get; }
    }
}