using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Aqueduct.Shared.Proxy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aqueduct.Shared.Serialisation
{
    public class JsonNetSerialisationDriver : ISerialisationDriver
    {
        private readonly List<Type> _primitiveJsonTypes = new() 
            { typeof(string), typeof(bool), typeof(int), typeof(decimal), typeof(double), typeof(float) };
        
        private readonly ITypeFinder _typeFinder;
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
        };

        private readonly TypeNameParser _typeNameParser = new();
        
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
                CheckForUnknownTypes(_typeNameParser.Parse(match.Groups[1].Value));
            }
        }

        private void CheckForUnknownTypes(TypeDescription typeDescription)
        {
            if (!typeDescription.AllGenericArgumentsSupplied)
            {
                throw new Exception("Cannot deserialise type with open type arguments");
            }
            
            var usableType = _typeFinder.GetUsableTypeByTypeDescription("Serialisable", typeDescription);

            if (!usableType.TypeDescription.AllGenericArgumentsSupplied)
            {
                for (var i = usableType.TypeDescription.GenericTypes.Count; i < typeDescription.GenericTypes.Count; i++)
                {
                    CheckForUnknownTypes(typeDescription.GenericTypes[i]);
                }
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
        
        public Exception DeserialiseException(byte[] serialised)
        {
            var serialisedString = Encoding.UTF8.GetString(serialised);
            
            CheckForUnknownTypes(serialisedString);

            var exceptionTree = JObject.Parse(serialisedString);
            var exception = JsonConvert.DeserializeObject(serialisedString, _serializerSettings);

            if (exception is not Exception)
            {
                throw new Exception("Could not deserialise Exception, serialised object was not an Exception");
            }

            var stackTrace = exceptionTree["StackTrace"]?.Value<string>();
            if (stackTrace != null)
            {
                var fieldInfo = typeof(Exception).GetField("_remoteStackTraceString", 
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);

                fieldInfo?.SetValue(exception, stackTrace);
            }
            
            return exception as Exception;
        }

        public object Deserialise(byte[] serialised, Type baseType)
        {
            object deserialised;
            if (_primitiveJsonTypes.Contains(baseType))
            {
                var serialisedString = Encoding.UTF8.GetString(serialised);
                deserialised = JsonConvert.DeserializeObject(serialisedString, baseType);
            }
            else
            {
                deserialised = Deserialise(serialised);
            }

            if (deserialised == null)
            {
                return null;
            }
            
            if (deserialised.GetType() != baseType && !deserialised.GetType().IsSubclassOf(baseType))
            {
                throw new Exception($"Serialised type was {deserialised.GetType()} but needed {baseType} or derived type");
            }

            return deserialised;
        }
    }
}