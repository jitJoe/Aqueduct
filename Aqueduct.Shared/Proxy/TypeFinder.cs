using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aqueduct.Shared.Proxy
{
    public class TypeFinder : ITypeFinder
    {
        private readonly Dictionary<string, Dictionary<string, Type>> _types 
            = new Dictionary<string, Dictionary<string, Type>>();

        public void RegisterTypeList(string purpose, ITypeList typeList)
        {
            RegisterAssemblies(purpose, typeList.GetAllowedAssemblies());
            RegisterTypes(purpose, typeList.GetAllowedTypes());
        }

        public void RegisterAssembly(string purpose, Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                RegisterType(purpose, type);
            }
        }

        public void RegisterAssemblies(string purpose, IReadOnlyCollection<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                RegisterAssembly(purpose, assembly);
            }
        }

        public void RegisterType(string purpose, Type type)
        {
            var purposeTypes = GetPurposeDictionary(purpose);
            if (!purposeTypes.ContainsKey(type.AssemblyQualifiedName))
            {
                purposeTypes.Add(type.AssemblyQualifiedName, type);
            }
        }

        public void RegisterTypes(string purpose, IReadOnlyCollection<Type> types)
        {
            foreach (var type in types)
            {
                RegisterType(purpose, type);
            }
        }

        public Type GetTypeByName(string purpose, string name)
        {
            var purposeTypes = GetPurposeDictionary(purpose);

            if (!purposeTypes.ContainsKey(name))
            {
                throw new Exception($"No {name} type found for {purpose}");
            }
            
            return purposeTypes[name];
        }

        public Type GetTypeByInterfaceImplementations(string purpose, List<Type> interfaceTypes)
        {
            var purposeTypes = GetPurposeDictionary(purpose);

            return purposeTypes.Values.FirstOrDefault(t =>
            {
                var interfaces = t.GetInterfaces();

                return interfaceTypes.All(it => interfaces.Contains(it));
            });
        }

        private Dictionary<string, Type> GetPurposeDictionary(string purpose)
        {
            if (! _types.ContainsKey(purpose))
            {
                _types[purpose] = new Dictionary<string, Type>();
            }
            
            return _types[purpose];
        }
    }
}