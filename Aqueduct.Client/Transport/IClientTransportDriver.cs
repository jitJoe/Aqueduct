using System.Threading.Tasks;
using Aqueduct.Shared.Proxy;

namespace Aqueduct.Client.Transport
{
    public interface IClientTransportDriver
    {
        ProxyInvocationHandler<ClientToServerInvocationMetaData> InvocationHandler { get; }

        Task StartAsync();
    }
}