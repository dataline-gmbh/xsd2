using System.CodeDom;
using System.IO;
using System.Linq;

using Xsd2.Capitalizers;
using Xunit;

namespace Xsd2.Tests
{
    public class ReportedIssuesTests
    {
        [Fact(Skip = "")]
        public void UppercaseNullablePropertiesAreGenerated()
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

            using (var o = File.CreateText(@"Schemas\Issue12.cs"))
            {
                var generator = new XsdCodeGenerator()
                {
                    Options = options,
                    OnValidateGeneratedCode = (ns, schema) =>
                    {
                        var upperCaseType = ns.Types.Cast<CodeTypeDeclaration>().Single(a => a.Name == "UpperCaseType");
                        var valueProp = (CodeMemberProperty)upperCaseType.Members.Cast<CodeTypeMember>().Single(a => a.Name == "Value");
                        Assert.Contains("Nullable", valueProp.Type.BaseType);
                    }
                };
                generator.Generate(new[] { @"Schemas\Issue12.xsd" }, o);
            }
        }
    }
}
