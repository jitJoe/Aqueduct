using Aqueduct.Shared.Proxy;

namespace Aqueduct.Client
{
    public class AqueductClientConfiguration
    {
        public int CallbackTimeoutMillis { get; set; } = 30_000;
        public ITypeList SerialisableTypeList { get; set; }
        public ITypeList ServicesTypeList { get; set; }
    }
}