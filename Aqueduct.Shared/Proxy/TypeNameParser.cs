using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aqueduct.Shared.Proxy
{
    public class TypeNameParser
    {
        public TypeDescription Parse(string assemblyQualifiedName)
        {
            assemblyQualifiedName = assemblyQualifiedName.Replace(", ", ",");
            
            var typeName = new StringBuilder();
            var arity = 0;
            var genericArgumentDescriptions = new List<TypeDescription>();
            var assemblyName = new StringBuilder();
            var assemblyVersion = new StringBuilder();
            var assemblyCulture = new StringBuilder();
            var publicKeyToken = new StringBuilder();

            var cursor = 0;
            char? previousCharacter = null;
            char? currentCharacter = assemblyQualifiedName[0];
            char? nextCharacter = assemblyQualifiedName[1];

            void MoveCursor(int amount)
            {
                cursor += amount;
                previousCharacter = cursor > 0 ? assemblyQualifiedName[cursor - 1] : null;
                currentCharacter = cursor < assemblyQualifiedName.Length ? assemblyQualifiedName[cursor] : null;
                nextCharacter = cursor < assemblyQualifiedName.Length - 1 ? assemblyQualifiedName[cursor + 1] : null;
            }

            void MoveCursorForward() => MoveCursor(1);

            while (currentCharacter != '`' && currentCharacter != ',')
            {
                typeName.Append(currentCharacter);
                MoveCursorForward();
            }

            MoveCursorForward();
            var arityString = new StringBuilder();
            if (previousCharacter == '`')
            {
                while (char.IsDigit(currentCharacter.Value))
                {
                    arityString.Append(currentCharacter);
                    MoveCursorForward();
                }

                arity = int.Parse(arityString.ToString());

                if (nextCharacter == '[')
                {
                    MoveCursor(2);
                    var unclosedSquareBrackets = 0;
                    while (genericArgumentDescriptions.Count < arity)
                    {
                        var nestedTypeDescription = new StringBuilder();
                        while (true)
                        {
                            nestedTypeDescription.Append(currentCharacter);
                            if (currentCharacter == '[')
                            {
                                unclosedSquareBrackets += 1;
                            }

                            if (currentCharacter == ']')
                            {
                                unclosedSquareBrackets -= 1;
                            }

                            if (nextCharacter == ']' && unclosedSquareBrackets == 0)
                            {
                                genericArgumentDescriptions.Add(Parse(nestedTypeDescription.ToString()));
                                
                                MoveCursor(3);
                                if (currentCharacter == '[')
                                {
                                    MoveCursorForward();
                                }

                                break;
                            }
                            
                            MoveCursorForward();
                        }
                    }
                    MoveCursorForward();
                }
                else
                {
                    MoveCursorForward();
                }
            }
            else
            {
                arity = 0;
            }
            
            while (currentCharacter != ',')
            {
                assemblyName.Append(currentCharacter);
                MoveCursorForward();
            }
            MoveCursorForward();
            while (currentCharacter != '=')
            {
                MoveCursorForward();
            }
            MoveCursorForward();
            while (currentCharacter != ',')
            {
                assemblyVersion.Append(currentCharacter);
                MoveCursorForward();
            }
            MoveCursorForward();
            while (currentCharacter != '=')
            {
                MoveCursorForward();
            }
            MoveCursorForward();
            while (currentCharacter != ',')
            {
                assemblyCulture.Append(currentCharacter);
                MoveCursorForward();
            }
            MoveCursorForward();
            while (currentCharacter != '=')
            {
                MoveCursorForward();
            }
            MoveCursorForward();
            while (currentCharacter != null)
            {
                publicKeyToken.Append(currentCharacter);
                MoveCursorForward();
            }

            return new TypeDescription
            {
                Name = typeName.ToString(),
                Arity = arity,
                AssemblyName = assemblyName.ToString(),
                AssemblyVersion = assemblyVersion.ToString(),
                AssemblyCulture = assemblyCulture.ToString(),
                PublicKeyToken = publicKeyToken.ToString(),
                GenericTypes = genericArgumentDescriptions
            };
        }
    }

    public class TypeDescription
    {
        public string Name { get; set; }
        public int? Arity { get; set; }
        public string AssemblyName { get; set; }
        public string AssemblyVersion { get; set; }
        public string AssemblyCulture { get; set; }
        public string PublicKeyToken { get; set; }
        public List<TypeDescription> GenericTypes { get; set; }
        public bool AllGenericArgumentsSupplied => GenericTypes.Count == Arity;

        protected bool Equals(TypeDescription other)
        {
            return Name == other.Name && 
                   Arity == other.Arity && 
                   AssemblyName == other.AssemblyName && 
                   AssemblyVersion == other.AssemblyVersion && 
                   AssemblyCulture == other.AssemblyCulture && 
                   PublicKeyToken == other.PublicKeyToken && 
                   GenericTypes.SequenceEqual(other.GenericTypes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TypeDescription) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Arity, AssemblyName, AssemblyVersion, AssemblyCulture, PublicKeyToken, GenericTypes);
        }
    }
}