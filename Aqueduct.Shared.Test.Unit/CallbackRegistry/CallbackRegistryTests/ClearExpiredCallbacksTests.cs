using System;
using System.Threading.Tasks;
using Xunit;

namespace Aqueduct.Shared.Test.Unit.CallbackRegistry.CallbackRegistryTests
{
    public class ClearExpiredCallbacksTests : CallbackRegistryTestsBase
    {
        [Fact]
        public void Clears_Expired_Callbacks()
        {
            _dateTimeProviderMock.Setup(dateTimeProvider => dateTimeProvider.Now())
                .Returns(DateTimeOffset.Now.AddMinutes(-1));
            
            var callbackTaskOne = _callbackRegistry.RegisterCallback(Guid.NewGuid(), null);
            
            _dateTimeProviderMock.Setup(dateTimeProvider => dateTimeProvider.Now())
                .Returns(DateTimeOffset.Now);
            
            var callbackTaskTwo = _callbackRegistry.RegisterCallback(Guid.NewGuid(), null);

            Assert.Equal(TaskStatus.WaitingForActivation, callbackTaskOne.Status);
            Assert.Equal(TaskStatus.WaitingForActivation, callbackTaskTwo.Status);

            _callbackRegistry.ClearExpiredCallbacks();
            
            Assert.Equal(TaskStatus.Canceled, callbackTaskOne.Status);
            Assert.Equal(TaskStatus.WaitingForActivation, callbackTaskTwo.Status);
        }
    }
}