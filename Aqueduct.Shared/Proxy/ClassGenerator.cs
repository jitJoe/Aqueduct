using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Aqueduct.Shared.Proxy
{
    public class ClassGenerator
    {
        private readonly string _name;
        private readonly TypeBuilder _typeBuilder;
        private readonly Type _baseClass;

        private readonly Dictionary<string, FieldBuilder> _fields = new Dictionary<string, FieldBuilder>();
        
        public ClassGenerator(ModuleBuilder moduleBuilder, string name, Type baseClass)
        {
            _name = name;
            _baseClass = baseClass;
            _typeBuilder = moduleBuilder.DefineType(name,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                baseClass);
        }

        public ClassGenerator(ModuleBuilder moduleBuilder, string name)
        {
            _name = name;
            _typeBuilder = moduleBuilder.DefineType(name,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout);
        }

        public void AddInterface(Type interfaceType)
        {
            _typeBuilder.AddInterfaceImplementation(interfaceType);
        }

        public void AddPrivateFieldsWithConstructor(Type[] fieldTypes, string[] fieldNames)
        {
            var fieldBuilders = new FieldBuilder[fieldTypes.Length];
            for (var i = 0; i < fieldTypes.Length; i++)
            {
                fieldBuilders[i] = _typeBuilder.DefineField(fieldNames[i], fieldTypes[i], FieldAttributes.Private);
                _fields.Add(fieldNames[i], fieldBuilders[i]);
            }
            
            var constructorBuilder = _typeBuilder.DefineConstructor(MethodAttributes.Public, 
                CallingConventions.HasThis, fieldTypes);

            var ilGenerator = constructorBuilder.GetILGenerator();
            
            for (int i = 0; i < fieldTypes.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_S, i + 1);
                ilGenerator.Emit(OpCodes.Stfld, fieldBuilders[i]);
            }

            ilGenerator.Emit(OpCodes.Ret);
        }

        public void AddProxyMethod(MethodInfo methodInfo, Type metadataType)
        {
            var methodBuilder = _typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType, 
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());

            var ilGenerator = methodBuilder.GetILGenerator();
            
            var getMethodFromHandle = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });
            var invocationHandlerInvokeMethod = typeof(ProxyInvocationHandler<>)
                .MakeGenericType(metadataType)
                .GetMethod("InvokeAsync");
            
            //Load _target onto eval stack
            ilGenerator.Emit(OpCodes.Ldarg_0);                                //this
            ilGenerator.Emit(OpCodes.Ldfld, _fields["_target"]);              //_target
            
            //Load methodInfo on eval stack
            ilGenerator.Emit(OpCodes.Ldtoken, methodInfo);                    //_target, 
            ilGenerator.Emit(OpCodes.Call, getMethodFromHandle);              //_target, target MethodInfo

            //Load object[] arguments onto eval stack
            var parameters = methodInfo.GetParameters();

            ilGenerator.Emit(OpCodes.Ldc_I4, parameters.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(object));

            for (var i = 0; i < parameters.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldc_I4, i);
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                if (parameters[i].ParameterType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, parameters[i].ParameterType);
                }

                ilGenerator.Emit(OpCodes.Stelem_Ref);
            }

            //Load metadata (from field _metadata) onto eval stack
            ilGenerator.Emit(OpCodes.Ldarg_0);                                //_target, target MethodInfo, object[], this
            ilGenerator.Emit(OpCodes.Ldfld, _fields["_metadata"]);            //_target, target MethodInfo, object[], _metadata
            
            //Call IProxyInvocationHandler/InvokeAsync
            ilGenerator.Emit(OpCodes.Callvirt, invocationHandlerInvokeMethod);

            ilGenerator.Emit(OpCodes.Ret);
        }
        
        public Type Get()
        {
            return _typeBuilder.CreateType();
        }
    }
}