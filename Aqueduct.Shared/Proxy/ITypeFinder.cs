using System;
using System.Collections.Generic;
using System.Reflection;

namespace Aqueduct.Shared.Proxy
{
    public interface ITypeFinder
    {
        void RegisterTypeList(string purpose, ITypeList typeList);
        void RegisterAssembly(string purpose, Assembly assembly);
        void RegisterAssemblies(string purpose, IReadOnlyCollection<Assembly> assemblies);
        
        void RegisterType(string purpose, Type type);
        void RegisterTypes(string purpose, IReadOnlyCollection<Type> types);

        Type GetTypeByName(string purpose, string name);
        Type GetTypeByInterfaceImplementations(string purpose, List<Type> interfaceTypes);
    }
}