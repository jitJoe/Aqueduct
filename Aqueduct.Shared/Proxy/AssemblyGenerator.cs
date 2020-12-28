using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Aqueduct.Shared.Proxy
{
    public class AssemblyGenerator
    {
        private readonly ModuleBuilder _moduleBuilder;

        public AssemblyGenerator(string name)
        {
            var assemblyName = new AssemblyName(name);

            _moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule("MainModule");
        }

        public ClassGenerator GetClassGenerator(string name) => new ClassGenerator(_moduleBuilder, name);
        public ClassGenerator GetClassGenerator(string name, Type baseClass) => new ClassGenerator(_moduleBuilder, name, baseClass);
    }
}