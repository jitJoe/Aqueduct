using System;
using System.Threading.Tasks;
using Xunit;

namespace Aqueduct.Shared.Test.Unit.CallbackRegistry.CallbackRegistryTests
{
    public class RegisterValuedCallbackTests : CallbackRegistryTestsBase
    {
        [Fact]
        public async Task Returns_Callback_Task()
        {
            Assert.IsType<Task<string>>(_callbackRegistry.RegisterValuedCallback<string>(Guid.NewGuid(), Guid.NewGuid()));
        }
        
        [Fact]
        public async Task Returns_Callback_Task_No_Connection_Id()
        {
            Assert.IsType<Task<string>>(_callbackRegistry.RegisterValuedCallback<string>(Guid.NewGuid(), null));
        }
    }
}