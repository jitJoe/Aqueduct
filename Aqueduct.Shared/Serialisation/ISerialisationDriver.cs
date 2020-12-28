using System;

namespace Aqueduct.Shared.Serialisation
{
    public interface ISerialisationDriver
    {
        byte[] Serialise(object subject);
        byte[] SerialiseException(Exception exception);
        object Deserialise(byte[] serialised);
        object Deserialise(byte[] serialised, Type baseType);
    }
}