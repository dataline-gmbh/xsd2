using System.Collections.Generic;
using System.IO;
using System.Text;

using Xsd2.Capitalizers;
using Xunit;

namespace Xsd2.Tests
{
    public class GenerationTests
    {
        [Fact]
        public void Test1()
        {
            var options = new XsdCodeGeneratorOptions
            {
                Imports = new List<string>() {@"Schemas\MetaConfig.xsd"},
                PropertyNameCapitalizer = new FirstCharacterCapitalizer(),
                OutputNamespace = "XSD2",
                UseLists = true,
                UseNullableTypes = true,
                ExcludeImportedTypes = true,
                AttributesToRemove =
                {
                    "System.Diagnostics.DebuggerStepThroughAttribute"
                }
            };

            using (var o = File.CreateText(@"Schemas\Xsd2Config.cs"))
            {
                var generator = new XsdCodeGenerator() { Options = options };
                generator.Generate(new[] { @"Schemas\Xsd2Config.xsd" }, o);
            }
        }

        [Fact(Skip = "")]
        public void Test2()
        {
            var options = new XsdCodeGeneratorOptions
            {
                PropertyNameCapitalizer = new FirstCharacterCapitalizer(),
                OutputNamespace = "XSD2",
                UseLists = true,
                UseNullableTypes = true,
                ExcludeImportedTypes = true,
                AttributesToRemove =
                {
                    "System.Diagnostics.DebuggerStepThroughAttribute"
                }
            };

            using (var o = File.CreateText(@"Schemas\Data.cs"))
            {
                var generator = new XsdCodeGenerator() { Options = options };
                generator.Generate(new[] { @"Schemas\Data.xsd" }, o);
            }
        }

        [Fact]
        public void Test3()
        {
            var options = new XsdCodeGeneratorOptions
            {
                PropertyNameCapitalizer = new FirstCharacterCapitalizer(),
                OutputNamespace = "XSD2",
                UseLists = true,
                UseNullableTypes = true,
                ExcludeImportedTypes = true,
                MixedContent = true,
                AttributesToRemove =
                {
                    "System.Diagnostics.DebuggerStepThroughAttribute"
                }
            };

            using (var o = File.CreateText(@"Schemas\Form.cs"))
            {
                var generator = new XsdCodeGenerator() { Options = options };
                generator.Generate(new[] { @"Schemas\Form.xsd" }, o);
            }
        }

        [Fact]
        public void TestNested()
        {
            var options = new XsdCodeGeneratorOptions
            {
                OutputNamespace = "XSD2",
                FixXsds = true
            };

            var sb = new StringBuilder();
            using (var o = new StringWriter(sb))
            {
                var generator = new XsdCodeGenerator() { Options = options };
                generator.Generate(new[] { @"Schemas\NestedAttributeGroup.xsd" }, o);
            }

            string result = sb.ToString();

            Assert.Contains("a1", result);
            Assert.Contains("a2", result);
            Assert.Contains("a3", result);
        }

    }
}
