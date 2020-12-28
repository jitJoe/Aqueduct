using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Aqueduct.Shared.Proxy;
using Newtonsoft.Json;

namespace Aqueduct.Shared.Serialisation
{
    public class JsonNetSerialisationDriver : ISerialisationDriver
    {
        private readonly ITypeFinder _typeFinder;
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        };

        public JsonNetSerialisationDriver(ITypeFinder typeFinder)
        {
            _typeFinder = typeFinder;
        }

        public byte[] Serialise(object subject)
        {
            var serialised = JsonConvert.SerializeObject(subject, _serializerSettings);

            CheckForUnknownTypes(serialised);

            return Encoding.UTF8.GetBytes(serialised);
        }

        private void CheckForUnknownTypes(string serialised)
        {
            var matches = Regex.Matches(serialised, "\"\\$type\":\"(.+?)\"");

            foreach (Match match in matches)
            {
                _typeFinder.GetTypeByName("Serialisable", match.Groups[1].Value);
            }
        }

        public byte[] SerialiseException(Exception exception)
        {
            var innerException = exception;
            
            if (exception is TargetInvocationException || exception is AggregateException)
            {
                innerException = exception.InnerException;
            }

            try
            {
                return Serialise(innerException);
            }
            catch (Exception _)
            {
                return Serialise(new Exception("Original Exception not serialisable, replaced.  " + exception.Message));
            }
        }

        public object Deserialise(byte[] serialised)
        {
            var serialisedString = Encoding.UTF8.GetString(serialised);
            
            CheckForUnknownTypes(serialisedString);

            return JsonConvert.DeserializeObject(serialisedString, _serializerSettings);
        }

        public object Deserialise(byte[] serialised, Type baseType)
        {
            var deserialised = Deserialise(serialised);

            if (deserialised.GetType() != baseType && !deserialised.GetType().IsSubclassOf(baseType))
            {
                throw new Exception($"Serialised type was {deserialised.GetType()} but needed {baseType} or derived type");
            }

            return deserialised;
        }
    }
}