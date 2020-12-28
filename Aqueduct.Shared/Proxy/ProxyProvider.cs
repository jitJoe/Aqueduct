using System;

namespace Aqueduct.Shared.Proxy
{
    public class ProxyProvider : IProxyProvider
    {
        private readonly AssemblyGenerator _assemblyGenerator = new AssemblyGenerator("DynamicAssembly");
        
        public Type GetProxyType<TProxyType, TMetaDataType>(ProxyInvocationHandler<TMetaDataType> invocationHandler) where TProxyType : class where TMetaDataType : class =>
            GetProxyType(typeof(TProxyType), typeof(TMetaDataType));

        public Type GetProxyType(Type typeToProxy, Type metaDataType)
        {
            var invocationHandlerType = typeof(ProxyInvocationHandler<>).MakeGenericType(metaDataType);
            
            var classGenerator = _assemblyGenerator.GetClassGenerator($"{Guid.NewGuid()}{typeToProxy.AssemblyQualifiedName}Proxy");
            classGenerator.AddInterface(typeToProxy);
            classGenerator.AddPrivateFieldsWithConstructor(new[] { invocationHandlerType, metaDataType }, new [] { "_target", "_metadata" });
            
            foreach (var methodInfo in typeToProxy.GetMethods())
            {
                classGenerator.AddProxyMethod(methodInfo, metaDataType);
            }
            
            return classGenerator.Get();
        }
    }
}