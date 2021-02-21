using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aqueduct.Shared.Proxy
{
    public class TypeFinder : ITypeFinder
    {
        private readonly Dictionary<string, Dictionary<string, List<UsableType>>> _types = new();
        private readonly TypeNameParser _typeNameParser = new();
        
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
            var description = _typeNameParser.Parse(type.AssemblyQualifiedName);
            var key = GetTypeKey(description);
            
            var purposeTypes = GetPurposeDictionary(purpose);
            if (!purposeTypes.ContainsKey(key))
            {
                purposeTypes.Add(key, new());
            }

            if (purposeTypes[key].All(usableType => usableType.Type != type))
            {
                purposeTypes[key].Add(new UsableType
                {
                    TypeDescription = description,
                    Type = type
                });
            }
        }

        public void RegisterTypes(string purpose, IReadOnlyCollection<Type> types)
        {
            foreach (var type in types)
            {
                RegisterType(purpose, type);
            }
        }

        public Type GetTypeByName(string purpose, string name) => GetUsableTypeByName(purpose, name).Type;

        public UsableType GetUsableTypeByName(string purpose, string name)
        {
            var purposeTypes = GetPurposeDictionary(purpose);
            var description = _typeNameParser.Parse(name);
            var key = GetTypeKey(description);
            
            if (!purposeTypes.ContainsKey(key))
            {
                throw new Exception($"No {name} type found for {purpose}");
            }

            var usableTypes = purposeTypes[key];

            var specific = usableTypes.FirstOrDefault(usableType =>
            {
                if (!usableType.TypeDescription.AllGenericArgumentsSupplied)
                {
                    return false;
                }

                for (var i = 0; i < usableType.TypeDescription.GenericTypes.Count; i++)
                {
                    var usableTypeGenericType = usableType.TypeDescription.GenericTypes[i];
                    var requestedTypeGenericType = description.GenericTypes[i];

                    if (! usableTypeGenericType.Equals(requestedTypeGenericType))
                    {
                        return false;
                    }
                }

                return true;
            });

            if (specific != null)
            {
                return specific;
            }

            var openType = usableTypes.FirstOrDefault(usableType =>
            {
                if (usableType.TypeDescription.AllGenericArgumentsSupplied)
                {
                    return false;
                }
                
                for (var i = 0; i < usableType.TypeDescription.GenericTypes.Count; i++)
                {
                    var usableTypeGenericType = usableType.TypeDescription.GenericTypes[i];
                    var requestedTypeGenericType = description.GenericTypes[i];

                    if (! usableTypeGenericType.Equals(requestedTypeGenericType))
                    {
                        return false;
                    }
                }

                return true;
            });

            if (openType != null)
            {
                return openType;
            }
            
            throw new Exception($"No {name} type found for {purpose}");
        }

        public UsableType GetUsableTypeByTypeDescription(string purpose, TypeDescription typeDescription)
        {
            var purposeTypes = GetPurposeDictionary(purpose);
            var key = GetTypeKey(typeDescription);
            
            if (!purposeTypes.ContainsKey(key))
            {
                throw new Exception($"No {typeDescription.Name} type found for {purpose}");
            }

            var usableTypes = purposeTypes[key];
            
            var specific = usableTypes.FirstOrDefault(usableType => usableType.TypeDescription.Equals(typeDescription));

            if (specific != null)
            {
                return specific;
            }
            
            var openType = usableTypes.FirstOrDefault(usableType =>
            {
                if (usableType.TypeDescription.AllGenericArgumentsSupplied)
                {
                    return false;
                }
                
                for (var i = 0; i < usableType.TypeDescription.GenericTypes.Count; i++)
                {
                    var usableTypeGenericType = usableType.TypeDescription.GenericTypes[i];
                    var requestedTypeGenericType = typeDescription.GenericTypes[i];

                    if (! usableTypeGenericType.Equals(requestedTypeGenericType))
                    {
                        return false;
                    }
                }

                return true;
            });

            if (openType != null)
            {
                return openType;
            }

            throw new Exception($"Could not find type {typeDescription.Name} by type description");
        }

        public Type GetTypeByInterfaceImplementations(string purpose, List<Type> interfaceTypes)
        {
            var purposeTypes = GetPurposeDictionary(purpose);

            return purposeTypes.Values.SelectMany(v => v).FirstOrDefault(t =>
            {
                var interfaces = t.Type.GetInterfaces();

                return interfaceTypes.All(it => interfaces.Contains(it));
            })?.Type;
        }

        private Dictionary<string, List<UsableType>> GetPurposeDictionary(string purpose)
        {
            if (! _types.ContainsKey(purpose))
            {
                _types[purpose] = new Dictionary<string, List<UsableType>>();
            }
            
            return _types[purpose];
        }

        private string GetTypeKey(TypeDescription typeDescription) => 
            $"{typeDescription.AssemblyName}-{typeDescription.AssemblyVersion}-{typeDescription.PublicKeyToken}-{typeDescription.Name}-{typeDescription.Arity}";
    }

    public class UsableType
    {
        public TypeDescription TypeDescription { get; set; }
        public Type Type { get; set; }
    }
}