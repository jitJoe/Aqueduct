using System;
using System.Threading.Tasks;
using Xunit;

namespace Aqueduct.Shared.Test.Unit.CallbackRegistry.CallbackRegistryTests
{
    public class PerformValuedCallbackTests : CallbackRegistryTestsBase
    {
        [Fact]
        public void Cannot_Find_Callback_Throws()
        {
            var exception = Assert.Throws<Exception>(() => 
                _callbackRegistry.PerformValuedCallback<string>(Guid.NewGuid(), "hello"));
            
            Assert.Equal("Could not get callback", exception.Message);
        }

        [Fact]
        public async Task Resolves_Awaited_Task()
        {
            var invocationId = Guid.NewGuid();
            
            var callbackTask = _callbackRegistry.RegisterValuedCallback<string>(invocationId);
            
            _callbackRegistry.PerformValuedCallback<string>(invocationId, "hello");

            Assert.Equal("hello", await callbackTask);
        }
    }
}