using System;
using System.Threading.Tasks;
using Xunit;

namespace Aqueduct.Shared.Test.Unit.CallbackRegistry.CallbackRegistryTests
{
    public class PerformCallbackTests : CallbackRegistryTestsBase
    {
        [Fact]
        public void Cannot_Find_Callback_Throws()
        {
            var exception = Assert.Throws<Exception>(() => 
                _callbackRegistry.PerformCallback(Guid.NewGuid()));
            
            Assert.Equal("Could not get callback", exception.Message);
        }

        [Fact]
        public async Task Resolves_Awaited_Task()
        {
            var invocationId = Guid.NewGuid();
            
            var callbackTask = _callbackRegistry.RegisterCallback(invocationId);
            
            _callbackRegistry.PerformCallback(invocationId);

            await callbackTask;
        }
    }
}