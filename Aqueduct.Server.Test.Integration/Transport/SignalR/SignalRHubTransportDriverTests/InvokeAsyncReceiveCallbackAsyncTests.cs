using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aqueduct.Server.Transport;
using Moq;
using Xunit;

namespace Aqueduct.Server.Test.Integration.Transport.SignalR.SignalRHubTransportDriverTests
{
    public class InvokeAsyncReceiveCallbackAsyncTests : SignalRHubTransportDriverTestsBase
    {
        [Fact]
        public async Task Invocation_Serialisation_Issue_Throws()
        {
            var connectionId = Guid.NewGuid();
            var signalRConnectionId = "12345";
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(Task.FromResult(signalRConnectionId));
            
            await StartServerAndClientAsync();
            
            var methodInfo = typeof(IService).GetMethod("Method");

            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Throws(new Exception("Could not serialise"));

            await Assert.ThrowsAsync<Exception>(async () =>
                await (Task) TestBridge.ServerTransportDriver.InvocationHandler
                    .InvokeAsync(methodInfo, new object[] {"argument"}, 
                        new ServerToClientInvocationMetaData { AqueductConnectionId = connectionId.ToString()}));
        }
        
        [Fact]
        public async Task Invocation_Sends_Non_Valued_Callback_To_Caller()
        {
            var connectionId = Guid.NewGuid();

            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();

            var methodInfo = typeof(IService).GetMethod("Method");

            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Returns(new byte[] { 1 });
            
            var taskCompletionSource = new TaskCompletionSource();
            TestBridge.CallbackRegistryMock.Setup(callbackRegistry => callbackRegistry.RegisterCallback(It.IsAny<Guid>(), null))
                .Returns(taskCompletionSource.Task);

            TestBridge.CallbackRegistryMock.Setup(callbackRegistry => callbackRegistry.GetCallbackReturnType(It.IsAny<Guid>()))
                .Returns((Type) null);

            TestBridge.CallbackRegistryMock.Setup(callbackRegistry =>
                    callbackRegistry.PerformCallback(It.IsAny<Guid>(), null))
                .Callback((Guid invocationId, Guid? connectionId) => taskCompletionSource.SetResult());
            
            _hubConnection.On("ReceiveInvocationAsync", new[] { typeof(Guid), typeof(string), typeof(string), typeof(List<String>), typeof(List<byte[]>) },  
                async (arguments, _) =>
                {
                    await _hubConnection.SendCoreAsync("ReceiveCallbackAsync", new object[] { arguments[0], null, null });
                }, null);
            
            await (Task) TestBridge.ServerTransportDriver.InvocationHandler
                .InvokeAsync(methodInfo, new object[] {"argument"}, 
                    new ServerToClientInvocationMetaData { AqueductConnectionId = connectionId.ToString() });
        }
        
        [Fact]
        public async Task Invocation_Sends_Non_Valued_Exception_Callback_To_Caller()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));

            await StartServerAndClientAsync();
            
            var methodInfo = typeof(IService).GetMethod("Method");

            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Returns(new byte[] { 1 });

            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.DeserialiseException(new byte[] {2}))
                .Returns(new AnException());

            var taskCompletionSource = new TaskCompletionSource();
            TestBridge.CallbackRegistryMock.Setup(callbackRegistry => callbackRegistry.RegisterCallback(It.IsAny<Guid>(), null))
                .Returns(taskCompletionSource.Task);

            TestBridge.CallbackRegistryMock.Setup(callbackRegistry => callbackRegistry.GetCallbackReturnType(It.IsAny<Guid>()))
                .Returns((Type) null);

            TestBridge.CallbackRegistryMock.Setup(callbackRegistry =>
                    callbackRegistry.ThrowForCallback(It.IsAny<Guid>(), It.IsAny<Exception>(), null))
                .Callback((Guid invocationId, Exception exception, Guid? connectionId) => taskCompletionSource.SetException(exception));

            _hubConnection.On("ReceiveInvocationAsync", new[] { typeof(Guid), typeof(string), typeof(string), typeof(List<String>), typeof(List<byte[]>) },  
                async (arguments, _) =>
                {
                    await _hubConnection.SendCoreAsync("ReceiveCallbackAsync", new object[] { arguments[0], null, new byte[] { 2 } });
                }, null);

            await Assert.ThrowsAsync<AnException>(async () =>
                await (Task) TestBridge.ServerTransportDriver.InvocationHandler
                .InvokeAsync(methodInfo, new object[] {"argument"}, 
                    new ServerToClientInvocationMetaData { AqueductConnectionId = connectionId.ToString() }));
        }
        
        [Fact]
        public async Task Invocation_Sends_Valued_Callback_To_Caller()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();
            
            var methodInfo = typeof(IService).GetMethod("ValueReturnMethod");

            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Returns(new byte[] { 1 });
            
            var taskCompletionSource = new TaskCompletionSource<string>();
            TestBridge.CallbackRegistryMock.Setup(callbackRegistry => callbackRegistry.RegisterValuedCallback<string>(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(taskCompletionSource.Task);

            TestBridge.CallbackRegistryMock.Setup(callbackRegistry => callbackRegistry.GetCallbackReturnType(It.IsAny<Guid>()))
                .Returns(typeof(string));

            TestBridge.CallbackRegistryMock.Setup(callbackRegistry =>
                    callbackRegistry.PerformValuedCallback<string>(It.IsAny<Guid>(), It.IsAny<string>(), null))
                .Callback((Guid invocationId, string value, Guid? connectionId) => taskCompletionSource.SetResult(value));

            _hubConnection.On("ReceiveInvocationAsync", new[] { typeof(Guid), typeof(string), typeof(string), typeof(List<String>), typeof(List<byte[]>) },  
                async (arguments, _) =>
                {
                    await _hubConnection.SendCoreAsync("ReceiveCallbackAsync", new object[] { arguments[0], new byte[] { 2 }, null });
                }, null);
            
            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Returns("return value");
            
            var result = await (Task<string>) TestBridge.ServerTransportDriver.InvocationHandler
                .InvokeAsync(methodInfo, new object[] {"argument"}, 
                    new ServerToClientInvocationMetaData { AqueductConnectionId = connectionId.ToString() });
            
            Assert.Equal("return value", result);
        }
        
        [Fact]
        public async Task Invocation_Sends_Valued_Exception_Callback_To_Caller()
        {
            var connectionId = Guid.NewGuid();
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetAqueductConnectionIdForSignalRConnectionIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(connectionId));
            
            TestBridge.ConnectionIdMappingRegistryMock.Setup(connectionIdMappingRegistry =>
                    connectionIdMappingRegistry.GetSignalRConnectionIdForAqueductConnectionIdAsync(connectionId))
                .Returns(() => Task.FromResult(_hubConnection.ConnectionId));
            
            await StartServerAndClientAsync();
            
            var methodInfo = typeof(IService).GetMethod("ValueReturnMethod");

            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Returns(new byte[] { 1 });
            
            var taskCompletionSource = new TaskCompletionSource<string>();
            TestBridge.CallbackRegistryMock.Setup(callbackRegistry => callbackRegistry.RegisterValuedCallback<string>(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(taskCompletionSource.Task);

            TestBridge.CallbackRegistryMock.Setup(callbackRegistry => callbackRegistry.GetCallbackReturnType(It.IsAny<Guid>()))
                .Returns(typeof(string));

            TestBridge.CallbackRegistryMock.Setup(callbackRegistry =>
                    callbackRegistry.ThrowForCallback(It.IsAny<Guid>(), It.IsAny<Exception>(), null))
                .Callback((Guid invocationId, Exception exception, Guid? connectionId) => taskCompletionSource.SetException(exception));

            _hubConnection.On("ReceiveInvocationAsync", new[] { typeof(Guid), typeof(string), typeof(string), typeof(List<String>), typeof(List<byte[]>) },  
                async (arguments, _) =>
                {
                    await _hubConnection.SendCoreAsync("ReceiveCallbackAsync", new object[] { arguments[0], null, new byte[] { 2 } });
                }, null);
            
            TestBridge.SerialisationDriverMock.Setup(serialisationDriver => serialisationDriver.DeserialiseException(new byte[] {2}))
                .Returns(new AnException());
            
            await Assert.ThrowsAsync<AnException>(async () => await (Task<string>) TestBridge.ServerTransportDriver.InvocationHandler
                .InvokeAsync(methodInfo, new object[] {"argument"}, 
                    new ServerToClientInvocationMetaData { AqueductConnectionId = connectionId.ToString() }));
        }
        
        public interface IService
        {
            Task Method(string argument);
            Task<string> ValueReturnMethod(string argument);
        }

        public class AnException : Exception
        {
            
        }
    }
}