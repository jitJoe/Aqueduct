using Aqueduct.Shared.Proxy;

namespace Aqueduct.Server
{
    public class AqueductServerConfiguration
    {
        public int CallbackTimeoutMillis { get; set; } = 30_000;
        public ITypeList SerialisableTypeList { get; set; }
        public ITypeList ServicesTypeList { get; set; }
    }
}