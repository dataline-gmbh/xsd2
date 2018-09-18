#if disabled

using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using Xsd2.Capitalizers;

namespace Xsd2.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication { Name = "Xsd2", FullName = "Xsd2Cli" };

            app.HelpOption("-?|-h|--help");
            var inputs = app.Argument("input", "Schemas to process", true);
            var outputDirectory = app.Option("-o|--out|--output", "Sets the output directory", CommandOptionType.SingleValue);
            var language = app.Option("-l|--language", "Sets the language to use for code generation (CS or VB)", CommandOptionType.SingleValue);
            var header = app.Option("--header", "Write file header", CommandOptionType.NoValue);
            var order = app.Option("--order", "Preserve the element order", CommandOptionType.NoValue);
            var enableDataBinding = app.Option("--edb|--enableDataBinding", "Implements INotifyPropertyChanged for all types", CommandOptionType.NoValue);
            var lists = app.Option("--lists", "Use lists", CommandOptionType.NoValue);
            var stripDebugAttributes = app.Option("--strip-debug-attributes", "Strip debug attributes", CommandOptionType.NoValue);
            var xlinq = app.Option("--xl|--xlinq", "Use XLinq", CommandOptionType.NoValue);
            var removeAttribute = app.Option("--ra|--remove-attribute", "Sets an attribute name to remove from the output", CommandOptionType.MultipleValue);
            var pcl = app.Option("--pcl", "Target a PCL (implies --xl)", CommandOptionType.NoValue);
            var capitalizeProperties = app.Option("--cp|--capitalize|--capitalize-properties", "Capitalize properties", CommandOptionType.NoValue);
            var capitalizeTypes = app.Option("--ct|--capitalize-types", "Capitalize types", CommandOptionType.NoValue);
            var capitalizeEnums = app.Option("--ce|--capitalize-enum-values", "Capitalize enum values", CommandOptionType.NoValue);
            var capitalizeAll = app.Option("--ca|--capitalize-all", "Capitalize properties, types, and enum values (--cp, --ct and --ce)", CommandOptionType.NoValue);
            var propertyCapitalizer = app.Option("--property-capitalizer", "Sets the capitalizer to use for the --cp option", CommandOptionType.SingleValue);
            var typeCapitalizer = app.Option("--type-capitalizer", "Sets the capitalizer to use for the --ct option", CommandOptionType.SingleValue);
            var enumCapitalizer = app.Option("--enum-capitalizer", "Sets the capitalizer to use for the --ce option", CommandOptionType.SingleValue);
            var mixed = app.Option("--mixed", "Support mixed content", CommandOptionType.NoValue);
            var namespaceName = app.Option("-n|--ns|--namespace", "Sets the output namespace", CommandOptionType.SingleValue);
            var import = app.Option("--import", "Adds import", CommandOptionType.MultipleValue);
            var usingNamespaces = app.Option("-u|--using", "Adds a namespace to use", CommandOptionType.MultipleValue);
            var excludeImports = app.Option("--ei|--exclude-imports", "Exclude imported types", CommandOptionType.NoValue);
            var excludeImportsByName = app.Option("--ein|--exclude-imports-by-name", "Exclude imported types by name", CommandOptionType.NoValue);
            var nullable = app.Option("--nullable", "Use nullable types", CommandOptionType.NoValue);
            var all = app.Option("--all", "Enable --stip-debug-attribtues --lists --nullable --ei --mixed --ca", CommandOptionType.NoValue);
            var combine = app.Option("--combine", "Combine output to a single file", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var options = new XsdCodeGeneratorOptions
                {
                    UseNullableTypes = nullable.HasValue() || all.HasValue(),
                    OutputNamespace = namespaceName.HasValue() ? namespaceName.Value() : "Xsd2",
                    MixedContent = mixed.HasValue() || all.HasValue(),
                    ExcludeImportedTypes = excludeImports.HasValue() || excludeImportsByName.HasValue() || all.HasValue(),
                    ExcludeImportedTypesByNameAndNamespace = excludeImportsByName.HasValue(),
                    Imports = import.Values,
                    UsingNamespaces = usingNamespaces.Values,
                    Language = language.HasValue() ?
                        (XsdCodeGeneratorOutputLanguage)Enum.Parse(typeof(XsdCodeGeneratorOutputLanguage), language.Value(), true) :
                        XsdCodeGeneratorOutputLanguage.CS,
                    WriteFileHeader = header.HasValue(),
                    PreserveOrder = order.HasValue(),
                    EnableDataBinding = enableDataBinding.HasValue(),
                    UseLists = lists.HasValue() || all.HasValue(),
                    UseXLinq = xlinq.HasValue() || pcl.HasValue(),
                    AttributesToRemove = new HashSet<string>(removeAttribute.Values)
                };

                if (capitalizeProperties.HasValue() || capitalizeAll.HasValue() || all.HasValue())
                    options.PropertyNameCapitalizer = GetCapitalizer(propertyCapitalizer.HasValue() ? propertyCapitalizer.Value() : null);
                if (capitalizeTypes.HasValue() || capitalizeAll.HasValue() || all.HasValue())
                    options.TypeNameCapitalizer = GetCapitalizer(typeCapitalizer.HasValue() ? typeCapitalizer.Value() : null);
                if (capitalizeEnums.HasValue() || capitalizeAll.HasValue() || all.HasValue())
                    options.EnumValueCapitalizer = GetCapitalizer(enumCapitalizer.HasValue() ? enumCapitalizer.Value() : null);

                if (pcl.HasValue())
                {
                    options.AttributesToRemove.Add("System.SerializableAttribute");
                    options.AttributesToRemove.Add("System.ComponentModel.DesignerCategoryAttribute");
                }

                if (stripDebugAttributes.HasValue() || all.HasValue())
                    options.AttributesToRemove.Add("System.Diagnostics.DebuggerStepThroughAttribute");

                string outputFileExtension;
                switch (options.Language)
                {
                    case XsdCodeGeneratorOutputLanguage.VB:
                        outputFileExtension = ".vb";
                        break;
                    default:
                        outputFileExtension = ".cs";
                        break;
                }

                var generator = new XsdCodeGenerator() { Options = options };

                try
                {
                    if (combine.HasValue())
                    {
                        String outputPath = null;
                        foreach (var path in inputs.Values)
                        {
                            var fileInfo = new FileInfo(path);

                            if (outputPath == null)
                            {
                                outputPath = Path.Combine(outputDirectory.HasValue() ? outputDirectory.Value() : fileInfo.DirectoryName, combine.Value());
                            }

                            Console.WriteLine(fileInfo.FullName);
                        }

                        Console.WriteLine(outputPath);

                        using (var output = File.CreateText(outputPath))
                            generator.Generate(inputs.Values, output);
                    }
                    else
                    {
                        foreach (var path in inputs.Values)
                        {
                            var fileInfo = new FileInfo(path);
                            var outputPath = Path.Combine(
                                outputDirectory.HasValue() ? outputDirectory.Value() : fileInfo.DirectoryName,
                                Path.ChangeExtension(fileInfo.Name, outputFileExtension));

                            Console.WriteLine(fileInfo.FullName);
                            Console.WriteLine(outputPath);

                            using (var output = File.CreateText(outputPath))
                                generator.Generate(new[] { path }, output);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("XSD2 code generation failed.");
                    Console.Error.Write(ex.ToString());
                    return 2;
                }

                return 0;
            });
            app.Execute(args);
        }

        private static ICapitalizer GetCapitalizer(string name)
        {
            if (string.IsNullOrEmpty(name))
                GetCapitalizer(null, null);

            int argumentSeparator = name.IndexOf(':');
            if (argumentSeparator == -1)
                return GetCapitalizer(name, null);

            return GetCapitalizer(name.Substring(0, argumentSeparator), name.Substring(argumentSeparator + 1));
        }

        private static ICapitalizer GetCapitalizer(string name, string argument)
        {
            if (string.IsNullOrEmpty(name))
                return new FirstCharacterCapitalizer();

            switch (name.ToLowerInvariant())
            {
                case "first-character":
                case "first-char":
                case "first":
                    return new FirstCharacterCapitalizer();
                case "none":
                    return new NoneCapitalizer();
                case "word":
                    if (!string.IsNullOrEmpty(argument))
                        return new WordCapitalizer(Convert.ToInt32(argument));
                    return new WordCapitalizer();
            }

            throw new NotSupportedException(string.Format("There is no capitalizer associated with the name {0}", name));
        }
    }
}
#endif