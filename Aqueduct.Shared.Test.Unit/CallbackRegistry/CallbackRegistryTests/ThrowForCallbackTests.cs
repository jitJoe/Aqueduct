using System;
using System.Threading.Tasks;
using Xunit;

namespace Aqueduct.Shared.Test.Unit.CallbackRegistry.CallbackRegistryTests
{
    public class ThrowForCallbackTests : CallbackRegistryTestsBase
    {
        [Fact]
        public void Cannot_Find_Callback_Throws()
        {
            var exception = Assert.Throws<Exception>(() => 
                _callbackRegistry.ThrowForCallback(Guid.NewGuid(), new Exception()));
            
            Assert.Equal("Could not get callback", exception.Message);
        }

        [Fact]
        public async Task Throws_On_Awaited_Task()
        {
            var invocationId = Guid.NewGuid();
            
            var callbackTask = _callbackRegistry.RegisterCallback(invocationId);
            
            _callbackRegistry.ThrowForCallback(invocationId, new Exception("Something went wrong"));

            var exception = await Assert.ThrowsAsync<Exception>(() => callbackTask);
            
            Assert.Equal("Something went wrong", exception.Message);
        }
    }
}