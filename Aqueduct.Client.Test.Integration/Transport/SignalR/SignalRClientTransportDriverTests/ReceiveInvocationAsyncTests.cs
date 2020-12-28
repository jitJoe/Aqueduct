using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aqueduct.Client.Test.Integration.Extensions;
using Moq;
using Xunit;

namespace Aqueduct.Client.Test.Integration.Transport.SignalR.SignalRClientTransportDriverTests
{
    public class ReceiveInvocationAsyncTests : SignalRClientTransportDriverTestsBase
    {
        [Fact]
        public async void Invocation_For_Service_With_No_Registered_Interface_Type_Logs_Error()
        {
            StartServer();

            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IUnknownService"))
                .Throws(new Exception("No IUnknownService type found for Services"));

            await _signalRClientTransportDriver.StartAsync();

            await SendInvocationAsync(Guid.NewGuid(), "IUnknownService", "AMethod", new List<string>(), new List<byte[]>());
            
            await Task.Delay(50);

            _loggerMock.VerifyErrorWasCalled("Exception whilst receiving invocation", "No IUnknownService type found for Services");
        }
        
        [Fact]
        public async void Invocation_For_Service_With_No_Registered_Implementation_Logs_Error()
        {
            StartServer();

            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            _clientServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetClientService(typeof(IKnownService)))
                .Throws(new Exception("Cannot find implementation for IKnownService"));
                
            await _signalRClientTransportDriver.StartAsync();

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AMethod", new List<string>(), new List<byte[]>());
            
            await Task.Delay(50);

            _loggerMock.VerifyErrorWasCalled("Exception whilst receiving invocation", "Cannot find implementation for IKnownService");
        }
        
        [Fact]
        public async void Invocation_Targeting_Method_With_Non_Task_Return_Type_Logs_Error()
        {
            StartServer();

            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            _clientServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetClientService(typeof(IKnownService)))
                .Returns(new IKnownServiceImpl());

            await _signalRClientTransportDriver.StartAsync();

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AMethod", new List<string>(), new List<byte[]>());
            
            await Task.Delay(50);

            _loggerMock.VerifyErrorWasCalled("Exception whilst receiving invocation", 
                "Service 'IKnownService' method 'AMethod' has non-Task return type - cannot be invoked");
        }
        
        [Fact]
        public async void Invocation_With_Malformed_Argument_Logs_Error()
        {
            StartServer();

            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            _clientServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetClientService(typeof(IKnownService)))
                .Returns(new IKnownServiceImpl());

            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Serialisable", "string"))
                .Returns(typeof(string));
            
            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Throws(new Exception("Could not deserialise"));
            
            await _signalRClientTransportDriver.StartAsync();

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AnotherMethod", new List<string> { "string" }, new List<byte[]> { new byte[] { 2 } });

            await Task.Delay(50);
            
            _loggerMock.VerifyErrorWasCalled("Exception whilst receiving invocation", 
                "Could not deserialise");
        }
        
        [Fact]
        public async void Invocation_Throws_Exception_Sends_Exception_Callback_To_Hub()
        {
            StartServer();
            
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            var mockServiceImpl = new Mock<IKnownService>();
            
            _clientServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetClientService(typeof(IKnownService)))
                .Returns(mockServiceImpl.Object);

            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Serialisable", "string"))
                .Returns(typeof(string));
            
            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Returns("a string");

            var invocationException = new Exception("invocation exception");
            
            mockServiceImpl.Setup(serviceImpl => serviceImpl.AnotherMethod("a string"))
                .Throws(invocationException);

            _serialisationDriverMock
                .Setup(serialisationDriver => serialisationDriver.SerialiseException(
                    It.Is<TargetInvocationException>(targetInvocationException => targetInvocationException.InnerException == invocationException)))
                .Returns(new byte[] { 3 });
            
            await _signalRClientTransportDriver.StartAsync();

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AnotherMethod", new List<string> { "string" }, new List<byte[]> { new byte[] { 2 } });

            await Task.Delay(50);

            var lastCallback = _testHubAccessor.CallbackInvocations.Last();
            Assert.Equal(new byte[] { 3 }, lastCallback.ExceptionValue);
            Assert.Null(lastCallback.ReturnValue);
        }
        
        [Fact]
        public async void Invocation_Sends_Non_Valued_Callback_To_Hub()
        {
            StartServer();
            
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            var mockServiceImpl = new Mock<IKnownService>();
            
            _clientServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetClientService(typeof(IKnownService)))
                .Returns(mockServiceImpl.Object);

            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Serialisable", "string"))
                .Returns(typeof(string));
            
            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Returns("a string");

            mockServiceImpl.Setup(serviceImpl => serviceImpl.AnotherMethod("a string"))
                .Returns(Task.CompletedTask);

            await _signalRClientTransportDriver.StartAsync();

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AnotherMethod", new List<string> { "string" }, new List<byte[]> { new byte[] { 2 } });

            await Task.Delay(50);

            var lastCallback = _testHubAccessor.CallbackInvocations.Last();
            Assert.Null(lastCallback.ExceptionValue);
            Assert.Null(lastCallback.ReturnValue);
        }
        
        [Fact]
        public async void Invocation_Sends_Valued_Callback_To_Hub()
        {
            StartServer();
            
            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            var mockServiceImpl = new Mock<IKnownService>();
            
            _clientServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetClientService(typeof(IKnownService)))
                .Returns(mockServiceImpl.Object);

            _typeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Serialisable", "string"))
                .Returns(typeof(string));
            
            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Returns("a string");

            mockServiceImpl.Setup(serviceImpl => serviceImpl.ValueReturnMethod("a string"))
                .Returns(Task.FromResult("result"));

            _serialisationDriverMock
                .Setup(serialisationDriver => serialisationDriver.Serialise("result"))
                .Returns(new byte[] { 3 });
            
            await _signalRClientTransportDriver.StartAsync();

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "ValueReturnMethod", new List<string> { "string" }, new List<byte[]> { new byte[] { 2 } });

            await Task.Delay(50);

            var lastCallback = _testHubAccessor.CallbackInvocations.Last();
            Assert.Null(lastCallback.ExceptionValue);
            Assert.Equal(new byte[] { 3 }, lastCallback.ReturnValue);
        }

        private async Task SendInvocationAsync(Guid invocationId, string service, string methodName, List<string> methodParameterTypes, List<byte[]> methodArguments)
        {
            await _testHubAccessor.HubContext.Clients.All.SendCoreAsync("ReceiveInvocationAsync", 
                new object[] { invocationId, service, methodName, methodParameterTypes, methodArguments });
        }

        public interface IKnownService
        {
            void AMethod();
            Task AnotherMethod(string argument);
            Task<string> ValueReturnMethod(string argument);
        }

        public class IKnownServiceImpl : IKnownService
        {
            public void AMethod()
            {
                
            }

            public Task AnotherMethod(string argument) => Task.CompletedTask;
            public Task<string> ValueReturnMethod(string argument) => Task.FromResult("hi");
        }
    }
}