using System;
using System.Reflection;

namespace Aqueduct.Shared.Proxy
{
    public class ProxyInvocationHandler<TMetaDataType> where TMetaDataType : class
    {
        private Func<MethodInfo, object[], TMetaDataType, object> _invokeAsync;

        public ProxyInvocationHandler(Func<MethodInfo, object[], TMetaDataType, object> invokeAsync)
        {
            _invokeAsync = invokeAsync;
        }

        public object InvokeAsync(MethodInfo methodInfo, object[] arguments, TMetaDataType metadata) =>
            _invokeAsync(methodInfo, arguments, metadata);
    }
}