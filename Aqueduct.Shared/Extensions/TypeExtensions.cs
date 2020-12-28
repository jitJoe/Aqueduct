using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aqueduct.Shared.Extensions
{
    public static class TypeExtensions
    {
        public static Type GetMethodReturnType(this object invokee, string methodName, IReadOnlyList<Type> methodParameterTypes)
        {
            var method = invokee.GetType().GetMethods().FirstOrDefault(methodInfo =>
                methodInfo.Name == methodName && methodInfo.GetParameters().Select(p => p.ParameterType).SequenceEqual(methodParameterTypes));

            if (method == null)
            {
                throw new Exception($"Could not find method '{methodName}' with parameter types {string.Join(", ", methodParameterTypes)}");
            }

            return method.ReturnType;
        }
        
        public static object InvokeMethod(this object invokee, string methodName, Type[] methodParameterTypes, object[] methodArguments)
        {
            var method = invokee.GetType().GetMethods().FirstOrDefault(methodInfo =>
                methodInfo.Name == methodName && methodInfo.GetParameters().Select(p => p.ParameterType).SequenceEqual(methodParameterTypes));

            if (method == null)
            {
                throw new Exception($"Could not find method '{methodName}' with parameter types {string.Join<Type>(", ", methodParameterTypes)}");
            }

            return method.Invoke(invokee, methodArguments.ToArray());
        }
    }
}