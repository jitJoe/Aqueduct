using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aqueduct.Client.Test.Integration.Extensions;
using Moq;
using Xunit;

namespace Aqueduct.Server.Test.Integration.Transport.SignalR.SignalRHubTransportDriverTests
{
    public class ReceiveInvocationAsyncTests : SignalRHubTransportDriverTestsBase
    {
        [Fact]
        public async void Invocation_For_Service_With_No_Registered_Interface_Type_Logs_Error()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IUnknownService"))
                .Throws(new Exception("No IUnknownService type found for Services"));

            await SendInvocationAsync(Guid.NewGuid(), "IUnknownService", "AMethod", new List<string>(), new List<byte[]>());
            
            await Task.Delay(50);

            TestBridge.HubLoggerMock.VerifyErrorWasCalled("Exception whilst receiving invocation", "No IUnknownService type found for Services");
        }
        
        [Fact]
        public async void Invocation_For_Service_With_No_Registered_Implementation_Logs_Error()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            TestBridge.ServerServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetServerServiceAsync(typeof(IKnownService), connectionId))
                .Throws(new Exception("Cannot find implementation for IKnownService"));

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AMethod", new List<string>(), new List<byte[]>());
            
            await Task.Delay(50);

            TestBridge.HubLoggerMock.VerifyErrorWasCalled("Exception whilst receiving invocation", "Cannot find implementation for IKnownService");
        }
        
        [Fact]
        public async void Invocation_Targeting_Method_With_Non_Task_Return_Type_Logs_Error()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            TestBridge.ServerServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetServerServiceAsync(typeof(IKnownService), connectionId))
                .ReturnsAsync(new IKnownServiceImpl());

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AMethod", new List<string>(), new List<byte[]>());
            
            await Task.Delay(50);

            TestBridge.HubLoggerMock.VerifyErrorWasCalled("Exception whilst receiving invocation", 
                "Service 'IKnownService' method 'AMethod' has non-Task return type - cannot be invoked");
        }
        
        [Fact]
        public async void Invocation_With_Malformed_Argument_Logs_Error()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            TestBridge.ServerServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetServerServiceAsync(typeof(IKnownService), connectionId))
                .ReturnsAsync(new IKnownServiceImpl());

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Serialisable", "string"))
                .Returns(typeof(string));
            
            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Throws(new Exception("Could not deserialise"));

            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AnotherMethod", new List<string> { "string" }, new List<byte[]> { new byte[] { 2 } });

            await Task.Delay(500);
            
            TestBridge.HubLoggerMock.VerifyErrorWasCalled("Exception whilst receiving invocation", 
                "Could not deserialise");
        }
        
        [Fact]
        public async void Invocation_Throws_Exception_Sends_Exception_Callback_To_Hub()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();
            
            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            var mockServiceImpl = new Mock<IKnownService>();
            
            TestBridge.ServerServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetServerServiceAsync(typeof(IKnownService), connectionId))
                .ReturnsAsync(mockServiceImpl.Object);

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Serialisable", "string"))
                .Returns(typeof(string));
            
            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Returns("a string");

            var invocationException = new Exception("invocation exception");
            
            mockServiceImpl.Setup(serviceImpl => serviceImpl.AnotherMethod("a string"))
                .Throws(invocationException);

            TestBridge.SerialisationDriverMock
                .Setup(serialisationDriver => serialisationDriver.SerialiseException(
                    It.Is<TargetInvocationException>(targetInvocationException => targetInvocationException.InnerException == invocationException)))
                .Returns(new byte[] { 3 });

            CallbackInvocation callbackInvocation = null;
            _hubConnection.On("ReceiveCallbackAsync", new[] { typeof(Guid), typeof(byte[]), typeof(byte[]) },  
                async (arguments, _) =>
                {
                    callbackInvocation = new CallbackInvocation
                    {
                        InvocationId = (Guid) arguments[0],
                        ReturnValue = (byte[]) arguments[1],
                        ExceptionValue = (byte[]) arguments[2]
                    };
                }, null);
            
            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AnotherMethod", new List<string> { "string" }, new List<byte[]> { new byte[] { 2 } });

            await Task.Delay(50);
            
            Assert.Equal(new byte[] { 3 }, callbackInvocation?.ExceptionValue);
            Assert.Null(callbackInvocation?.ReturnValue);
        }
        
        [Fact]
        public async void Invocation_Sends_Non_Valued_Callback_To_Hub()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();
            
            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            var mockServiceImpl = new Mock<IKnownService>();
            
            TestBridge.ServerServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetServerServiceAsync(typeof(IKnownService), connectionId))
                .ReturnsAsync(mockServiceImpl.Object);

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Serialisable", "string"))
                .Returns(typeof(string));
            
            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Returns("a string");

            mockServiceImpl.Setup(serviceImpl => serviceImpl.AnotherMethod("a string"))
                .Returns(Task.CompletedTask);

            CallbackInvocation callbackInvocation = null;
            _hubConnection.On("ReceiveCallbackAsync", new[] { typeof(Guid), typeof(byte[]), typeof(byte[]) },  
                async (arguments, _) =>
                {
                    callbackInvocation = new CallbackInvocation
                    {
                        InvocationId = (Guid) arguments[0],
                        ReturnValue = (byte[]) arguments[1],
                        ExceptionValue = (byte[]) arguments[2]
                    };
                }, null);
            
            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "AnotherMethod", new List<string> { "string" }, new List<byte[]> { new byte[] { 2 } });

            await Task.Delay(50);
            
            Assert.Null(callbackInvocation.ExceptionValue);
            Assert.Null(callbackInvocation.ReturnValue);
        }
        
        [Fact]
        public async void Invocation_Sends_Valued_Callback_To_Hub()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();

            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Services", "IKnownService"))
                .Returns(typeof(IKnownService));

            var mockServiceImpl = new Mock<IKnownService>();
            
            TestBridge.ServerServiceProviderMock.Setup(clientServiceProvider => clientServiceProvider.GetServerServiceAsync(typeof(IKnownService), connectionId))
                .ReturnsAsync(mockServiceImpl.Object);

            TestBridge.TypeFinderMock.Setup(typeFinder => typeFinder.GetTypeByName("Serialisable", "string"))
                .Returns(typeof(string));
            
            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Returns("a string");

            mockServiceImpl.Setup(serviceImpl => serviceImpl.ValueReturnMethod("a string"))
                .Returns(Task.FromResult("result"));

            TestBridge.SerialisationDriverMock
                .Setup(serialisationDriver => serialisationDriver.Serialise("result"))
                .Returns(new byte[] { 3 });
            
            CallbackInvocation callbackInvocation = null;
            _hubConnection.On("ReceiveCallbackAsync", new[] { typeof(Guid), typeof(byte[]), typeof(byte[]) },
                async (arguments, _) =>
                {
                    callbackInvocation = new CallbackInvocation
                    {
                        InvocationId = (Guid) arguments[0],
                        ReturnValue = (byte[]) arguments[1],
                        ExceptionValue = (byte[]) arguments[2]
                    };
                }, null);
            
            await SendInvocationAsync(Guid.NewGuid(), "IKnownService", "ValueReturnMethod", new List<string> { "string" }, new List<byte[]> { new byte[] { 2 } });

            await Task.Delay(50);
            
            Assert.Null(callbackInvocation.ExceptionValue);
            Assert.Equal(new byte[] { 3 }, callbackInvocation.ReturnValue);
        }

        private async Task SendInvocationAsync(Guid invocationId, string service, string methodName, List<string> methodParameterTypes, List<byte[]> methodArguments)
        {
            await _hubConnection.SendCoreAsync("ReceiveInvocationAsync", 
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
        
        public class CallbackInvocation
        {
            public Guid InvocationId {get; set; }
            public byte[] ReturnValue { get; set; }
            public byte[] ExceptionValue { get; set; }
        }
    }
}