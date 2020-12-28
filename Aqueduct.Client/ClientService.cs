using Aqueduct.Client.ServiceProvider;

namespace Aqueduct.Client
{
    public class ClientService
    {
        public IClientServiceProvider ClientServiceProvider { get; internal set; }
    }
}