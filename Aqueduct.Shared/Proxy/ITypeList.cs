using System;
using System.Collections.Generic;
using System.Reflection;

namespace Aqueduct.Shared.Proxy
{
    public interface ITypeList
    {
        List<Type> GetAllowedTypes();
        List<Assembly> GetAllowedAssemblies();
    }
}