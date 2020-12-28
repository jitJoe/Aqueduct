using Aqueduct.Shared.DateTime;
using Moq;

namespace Aqueduct.Shared.Test.Unit.CallbackRegistry.CallbackRegistryTests
{
    public class CallbackRegistryTestsBase
    {
        protected readonly Shared.CallbackRegistry.CallbackRegistry _callbackRegistry;
        protected readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();

        public CallbackRegistryTestsBase()
        {
            _callbackRegistry = new Shared.CallbackRegistry.CallbackRegistry(new AqueductSharedConfiguration(), _dateTimeProviderMock.Object);
        }
    }
}