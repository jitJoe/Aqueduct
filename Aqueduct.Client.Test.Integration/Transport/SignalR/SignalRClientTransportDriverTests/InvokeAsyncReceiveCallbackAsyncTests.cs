using System;
using System.Threading.Tasks;
using Aqueduct.Client.Transport;
using Moq;
using Xunit;

namespace Aqueduct.Client.Test.Integration.Transport.SignalR.SignalRClientTransportDriverTests
{
    public class InvokeAsyncReceiveCallbackAsyncTests : SignalRClientTransportDriverTestsBase
    {
        [Fact]
        public async Task Invocation_Serialisation_Issue_Throws()
        {
            var methodInfo = typeof(IService).GetMethod("Method");

            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Throws(new Exception("Could not serialise"));

            await Assert.ThrowsAsync<Exception>(async () =>
                await (Task) _signalRClientTransportDriver.InvocationHandler
                    .InvokeAsync(methodInfo, new object[] {"argument"}, new ClientToServerInvocationMetaData()));
        }
        
        [Fact]
        public async Task Invocation_Sends_Non_Valued_Callback_To_Caller()
        {
            var methodInfo = typeof(IService).GetMethod("Method");

            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Returns(new byte[] { 1 });
            
            var taskCompletionSource = new TaskCompletionSource();
            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.RegisterCallback(It.IsAny<Guid>(), null))
                .Returns(taskCompletionSource.Task);

            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.GetCallbackReturnType(It.IsAny<Guid>()))
                .Returns((Type) null);

            _callbackRegistryMock.Setup(callbackRegistry =>
                    callbackRegistry.PerformCallback(It.IsAny<Guid>(), null))
                .Callback((Guid invocationId, Guid? connectionId) => taskCompletionSource.SetResult());

            StartServer();
            await _signalRClientTransportDriver.StartAsync();
            
            _testHubAccessor.OnReceiveInvocationAsync = 
                async (invocationId, serviceName, methodName, methodParameterTypes, methodArguments) =>
                {
                    await _testHubAccessor.HubContext.Clients.All.SendCoreAsync("ReceiveCallbackAsync", new object[] { invocationId, null, null });
                };
            
            await (Task) _signalRClientTransportDriver.InvocationHandler
                .InvokeAsync(methodInfo, new object[] {"argument"}, new ClientToServerInvocationMetaData());
        }
        
        [Fact]
        public async Task Invocation_Sends_Non_Valued_Exception_Callback_To_Caller()
        {
            var methodInfo = typeof(IService).GetMethod("Method");

            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Returns(new byte[] { 1 });

            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.DeserialiseException(new byte[] {2}))
                .Returns(new AnException());

            var taskCompletionSource = new TaskCompletionSource();
            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.RegisterCallback(It.IsAny<Guid>(), null))
                .Returns(taskCompletionSource.Task);

            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.GetCallbackReturnType(It.IsAny<Guid>()))
                .Returns((Type) null);

            _callbackRegistryMock.Setup(callbackRegistry =>
                    callbackRegistry.ThrowForCallback(It.IsAny<Guid>(), It.IsAny<Exception>(), null))
                .Callback((Guid invocationId, Exception exception, Guid? connectionId) => taskCompletionSource.SetException(exception));

            StartServer();
            await _signalRClientTransportDriver.StartAsync();
            
            _testHubAccessor.OnReceiveInvocationAsync = 
                async (invocationId, serviceName, methodName, methodParameterTypes, methodArguments) =>
                {
                    await _testHubAccessor.HubContext.Clients.All.SendCoreAsync("ReceiveCallbackAsync", new object[] { invocationId, null, new byte[] { 2 } });
                };
            
            await Assert.ThrowsAsync<AnException>(async () =>
                await (Task) _signalRClientTransportDriver.InvocationHandler
                .InvokeAsync(methodInfo, new object[] {"argument"}, new ClientToServerInvocationMetaData()));
        }
        
        [Fact]
        public async Task Invocation_Sends_Valued_Callback_To_Caller()
        {
            var methodInfo = typeof(IService).GetMethod("ValueReturnMethod");

            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Returns(new byte[] { 1 });
            
            var taskCompletionSource = new TaskCompletionSource<string>();
            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.RegisterValuedCallback<string>(It.IsAny<Guid>(), null))
                .Returns(taskCompletionSource.Task);

            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.GetCallbackReturnType(It.IsAny<Guid>()))
                .Returns(typeof(string));

            _callbackRegistryMock.Setup(callbackRegistry =>
                    callbackRegistry.PerformValuedCallback<string>(It.IsAny<Guid>(), It.IsAny<string>(), null))
                .Callback((Guid invocationId, string value, Guid? connectionId) => taskCompletionSource.SetResult(value));

            StartServer();
            await _signalRClientTransportDriver.StartAsync();
            
            _testHubAccessor.OnReceiveInvocationAsync = 
                async (invocationId, serviceName, methodName, methodParameterTypes, methodArguments) =>
                {
                    await _testHubAccessor.HubContext.Clients.All.SendCoreAsync("ReceiveCallbackAsync", new object[] { invocationId, new byte[] { 2 }, null });
                };

            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Deserialise(new byte[] {2}, typeof(string)))
                .Returns("return value");
            
            var result = await (Task<string>) _signalRClientTransportDriver.InvocationHandler
                .InvokeAsync(methodInfo, new object[] {"argument"}, new ClientToServerInvocationMetaData());
            
            Assert.Equal("return value", result);
        }
        
        [Fact]
        public async Task Invocation_Sends_Valued_Exception_Callback_To_Caller()
        {
            var methodInfo = typeof(IService).GetMethod("ValueReturnMethod");

            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.Serialise("argument"))
                .Returns(new byte[] { 1 });
            
            var taskCompletionSource = new TaskCompletionSource<string>();
            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.RegisterValuedCallback<string>(It.IsAny<Guid>(), null))
                .Returns(taskCompletionSource.Task);

            _callbackRegistryMock.Setup(callbackRegistry => callbackRegistry.GetCallbackReturnType(It.IsAny<Guid>()))
                .Returns(typeof(string));

            _callbackRegistryMock.Setup(callbackRegistry =>
                    callbackRegistry.ThrowForValuedCallback<string>(It.IsAny<Guid>(), It.IsAny<Exception>(), null))
                .Callback((Guid invocationId, Exception exception, Guid? connectionId) => taskCompletionSource.SetException(exception));

            StartServer();
            await _signalRClientTransportDriver.StartAsync();
            
            _testHubAccessor.OnReceiveInvocationAsync = 
                async (invocationId, serviceName, methodName, methodParameterTypes, methodArguments) =>
                {
                    await _testHubAccessor.HubContext.Clients.All.SendCoreAsync("ReceiveCallbackAsync", new object[] { invocationId, null, new byte[] { 2 } });
                };

            _serialisationDriverMock.Setup(serialisationDriver => serialisationDriver.DeserialiseException(new byte[] {2}))
                .Returns(new AnException());
            
            await Assert.ThrowsAsync<AnException>(async () => await (Task<string>) _signalRClientTransportDriver.InvocationHandler
                .InvokeAsync(methodInfo, new object[] {"argument"}, new ClientToServerInvocationMetaData()));
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