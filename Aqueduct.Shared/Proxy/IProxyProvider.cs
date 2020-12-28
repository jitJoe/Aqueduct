using System;

namespace Aqueduct.Shared.Proxy
{
    public interface IProxyProvider
    {
        Type GetProxyType<TProxyType, TMetaDataType>(ProxyInvocationHandler<TMetaDataType> invocationHandler) where TProxyType : class where TMetaDataType : class;

        Type GetProxyType(Type typeToProxy, Type metaDataType);
    }
}