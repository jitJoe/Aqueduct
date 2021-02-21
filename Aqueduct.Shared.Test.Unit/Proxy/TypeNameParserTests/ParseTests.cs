using Aqueduct.Shared.Proxy;
using Xunit;

namespace Aqueduct.Shared.Test.Unit.Proxy.TypeNameParserTests
{
    public class ParseTests
    {
        [Fact]
        public void Non_Generic_Type_Parses()
        {
            var description = (new TypeNameParser()).Parse(
                "System.Int32, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            
            Assert.Equal("System.Int32", description.Name);
            Assert.Equal(0, description.Arity);
            Assert.Equal("System.Private.CoreLib", description.AssemblyName);
            Assert.Equal("5.0.0.0", description.AssemblyVersion);
            Assert.Equal("neutral", description.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", description.PublicKeyToken);
            Assert.Empty(description.GenericTypes);
        }

        [Fact]
        public void Unspecified_Generic_Type_Parses()
        {
            var description = (new TypeNameParser()).Parse(
                "System.Collections.Generic.Dictionary`2, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            
            Assert.Equal("System.Collections.Generic.Dictionary", description.Name);
            Assert.Equal(2, description.Arity);
            Assert.Equal("System.Private.CoreLib", description.AssemblyName);
            Assert.Equal("5.0.0.0", description.AssemblyVersion);
            Assert.Equal("neutral", description.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", description.PublicKeyToken);
            Assert.Empty(description.GenericTypes);
        }
        
        [Fact]
        public void Single_Specified_Generic_Type_Parses()
        {
            var description = (new TypeNameParser()).Parse(
                "System.Collections.Generic.List`1[[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            
            Assert.Equal("System.Collections.Generic.List", description.Name);
            Assert.Equal(1, description.Arity);
            Assert.Equal("System.Private.CoreLib", description.AssemblyName);
            Assert.Equal("5.0.0.0", description.AssemblyVersion);
            Assert.Equal("neutral", description.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", description.PublicKeyToken);
            Assert.Single(description.GenericTypes);

            var genericArgumentDescription = description.GenericTypes[0];
            
            Assert.Equal("System.String", genericArgumentDescription.Name);
            Assert.Equal(0, genericArgumentDescription.Arity);
            Assert.Equal("System.Private.CoreLib", genericArgumentDescription.AssemblyName);
            Assert.Equal("5.0.0.0", genericArgumentDescription.AssemblyVersion);
            Assert.Equal("neutral", genericArgumentDescription.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", genericArgumentDescription.PublicKeyToken);
            Assert.Empty(genericArgumentDescription.GenericTypes);
        }
        
        [Fact]
        public void Multiple_Specified_Generic_Type_Parses()
        {
            var description = (new TypeNameParser()).Parse(
                "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            
            Assert.Equal("System.Collections.Generic.Dictionary", description.Name);
            Assert.Equal(2, description.Arity);
            Assert.Equal("System.Private.CoreLib", description.AssemblyName);
            Assert.Equal("5.0.0.0", description.AssemblyVersion);
            Assert.Equal("neutral", description.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", description.PublicKeyToken);
            Assert.Equal(2, description.GenericTypes.Count);

            var genericArgumentDescriptionOne = description.GenericTypes[0];
            var genericArgumentDescriptionTwo = description.GenericTypes[1];
            
            Assert.Equal("System.String", genericArgumentDescriptionOne.Name);
            Assert.Equal(0, genericArgumentDescriptionOne.Arity);
            Assert.Equal("System.Private.CoreLib", genericArgumentDescriptionOne.AssemblyName);
            Assert.Equal("5.0.0.0", genericArgumentDescriptionOne.AssemblyVersion);
            Assert.Equal("neutral", genericArgumentDescriptionOne.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", genericArgumentDescriptionOne.PublicKeyToken);
            Assert.Empty(genericArgumentDescriptionOne.GenericTypes);
            
            Assert.Equal("System.String", genericArgumentDescriptionTwo.Name);
            Assert.Equal(0, genericArgumentDescriptionTwo.Arity);
            Assert.Equal("System.Private.CoreLib", genericArgumentDescriptionTwo.AssemblyName);
            Assert.Equal("5.0.0.0", genericArgumentDescriptionTwo.AssemblyVersion);
            Assert.Equal("neutral", genericArgumentDescriptionTwo.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", genericArgumentDescriptionTwo.PublicKeyToken);
            Assert.Empty(genericArgumentDescriptionTwo.GenericTypes);
        }

        [Fact]
        public void Single_Nested_Specified_Generic_Type_Parses()
        {
            var description = (new TypeNameParser()).Parse(
                "System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            
            Assert.Equal("System.Collections.Generic.List", description.Name);
            Assert.Equal(1, description.Arity);
            Assert.Equal("System.Private.CoreLib", description.AssemblyName);
            Assert.Equal("5.0.0.0", description.AssemblyVersion);
            Assert.Equal("neutral", description.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", description.PublicKeyToken);
            Assert.Single(description.GenericTypes);

            var genericArgumentDescription = description.GenericTypes[0];

            Assert.Equal("System.Collections.Generic.List", genericArgumentDescription.Name);
            Assert.Equal(1, genericArgumentDescription.Arity);
            Assert.Equal("System.Private.CoreLib", genericArgumentDescription.AssemblyName);
            Assert.Equal("5.0.0.0", genericArgumentDescription.AssemblyVersion);
            Assert.Equal("neutral", genericArgumentDescription.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", genericArgumentDescription.PublicKeyToken);
            Assert.Single(genericArgumentDescription.GenericTypes);
            
            var nestedGenericArgumentDescription = genericArgumentDescription.GenericTypes[0];

            Assert.Equal("System.String", nestedGenericArgumentDescription.Name);
            Assert.Equal(0, nestedGenericArgumentDescription.Arity);
            Assert.Equal("System.Private.CoreLib", nestedGenericArgumentDescription.AssemblyName);
            Assert.Equal("5.0.0.0", nestedGenericArgumentDescription.AssemblyVersion);
            Assert.Equal("neutral", nestedGenericArgumentDescription.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", nestedGenericArgumentDescription.PublicKeyToken);
            Assert.Empty(nestedGenericArgumentDescription.GenericTypes);
        }

        [Fact]
        public void Double_Nested_Specified_Generic_Type_Parses()
        {
            var description = (new TypeNameParser()).Parse(
                "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Collections.Generic.List`1[[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            
            Assert.Equal("System.Collections.Generic.Dictionary", description.Name);
            Assert.Equal(2, description.Arity);
            Assert.Equal("System.Private.CoreLib", description.AssemblyName);
            Assert.Equal("5.0.0.0", description.AssemblyVersion);
            Assert.Equal("neutral", description.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", description.PublicKeyToken);
            Assert.Equal(2, description.GenericTypes.Count);

            var genericArgumentDescriptionOne = description.GenericTypes[0];
            var genericArgumentDescriptionTwo = description.GenericTypes[1];

            Assert.Equal("System.String", genericArgumentDescriptionOne.Name);
            Assert.Equal(0, genericArgumentDescriptionOne.Arity);
            Assert.Equal("System.Private.CoreLib", genericArgumentDescriptionOne.AssemblyName);
            Assert.Equal("5.0.0.0", genericArgumentDescriptionOne.AssemblyVersion);
            Assert.Equal("neutral", genericArgumentDescriptionOne.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", genericArgumentDescriptionOne.PublicKeyToken);
            Assert.Empty(genericArgumentDescriptionOne.GenericTypes);
            
            Assert.Equal("System.Collections.Generic.List", genericArgumentDescriptionTwo.Name);
            Assert.Equal(1, genericArgumentDescriptionTwo.Arity);
            Assert.Equal("System.Private.CoreLib", genericArgumentDescriptionTwo.AssemblyName);
            Assert.Equal("5.0.0.0", genericArgumentDescriptionTwo.AssemblyVersion);
            Assert.Equal("neutral", genericArgumentDescriptionTwo.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", genericArgumentDescriptionTwo.PublicKeyToken);
            Assert.Single(genericArgumentDescriptionTwo.GenericTypes);
            
            var nestedGenericArgumentDescription = genericArgumentDescriptionTwo.GenericTypes[0];

            Assert.Equal("System.String", nestedGenericArgumentDescription.Name);
            Assert.Equal(0, nestedGenericArgumentDescription.Arity);
            Assert.Equal("System.Private.CoreLib", nestedGenericArgumentDescription.AssemblyName);
            Assert.Equal("5.0.0.0", nestedGenericArgumentDescription.AssemblyVersion);
            Assert.Equal("neutral", nestedGenericArgumentDescription.AssemblyCulture);
            Assert.Equal("7cec85d7bea7798e", nestedGenericArgumentDescription.PublicKeyToken);
            Assert.Empty(nestedGenericArgumentDescription.GenericTypes);
        }
    }
}