using System;
using System.Collections.Generic;
using Xunit;

namespace Aqueduct.Shared.Test.Unit.CallbackRegistry.CallbackRegistryTests
{
    public class GetCallbackReturnTypeTests : CallbackRegistryTestsBase
    {
        [Fact]
        public void Throws_For_Unknown_Invocation_Id()
        {
            var exception = Assert.Throws<Exception>(() => _callbackRegistry.GetCallbackReturnType(Guid.NewGuid()));
            
            Assert.Equal("Could not get callback return type", exception.Message);
        }
        
        [Fact]
        public void Returns_Correct_Return_Type()
        {
            var invocationId = Guid.NewGuid();

            _callbackRegistry.RegisterValuedCallback<List<string>>(invocationId, null);
            
            Assert.Equal(typeof(List<string>), _callbackRegistry.GetCallbackReturnType(invocationId));
        }
    }
}