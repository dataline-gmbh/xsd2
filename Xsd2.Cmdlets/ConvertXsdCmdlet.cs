using System.IO;
using System.Linq;
using System.Management.Automation;
using Xsd2.Capitalizers;

namespace Xsd2.Cmdlets
{
    [Cmdlet(VerbsData.Convert, "Xsd", DefaultParameterSetName = ExplicitParameterSetName)]
    public class ConvertXsdCmdlet : PSCmdlet
    {
        private const string ExplicitParameterSetName = "Explicit";
        private const string AllParameterSetName = "All";

        [Parameter(HelpMessage = "Sets the output directory")]
        public string OutputDirectory { get; set; }

        [Parameter(HelpMessage = "Sets the language to use for code generation")]
        public XsdCodeGeneratorOutputLanguage Language { get; set; }

        [Parameter(HelpMessage = "Write file header")]
        public SwitchParameter WriteHeader { get; set; }

        [Parameter(HelpMessage = "Preserve the element order")]
        public SwitchParameter PreserveOrder { get; set; }

        [Parameter(HelpMessage = "Implements INotifyPropertyChanged for all types")]
        public SwitchParameter EnableDataBinding { get; set; }

        [Parameter(ParameterSetName = ExplicitParameterSetName, HelpMessage = "Use lists instead of arrays")]
        public SwitchParameter UseLists { get; set; }

        [Parameter(ParameterSetName = ExplicitParameterSetName, HelpMessage = "Strip debug attributes")]
        public SwitchParameter StripDebugAttributes { get; set; }

        [Parameter(HelpMessage = "Use Xlinq")]
        public SwitchParameter UseXlinq { get; set; }

        [Parameter(HelpMessage = "Remove attributes")]
        public string[] RemoveAttributes { get; set; }

        [Parameter(HelpMessage = "Target a PCL")]
        public SwitchParameter PclTarget { get; set; }

        [Parameter(HelpMessage = "Sets the type capitalizer to use")]
        public ICapitalizer TypeCapitalizer { get; set; }

        [Parameter(HelpMessage = "Sets the enum capitalizer to use")]
        public ICapitalizer EnumCapitalizer { get; set; }

        [Parameter(HelpMessage = "Sets the property capitalizer to use")]
        public ICapitalizer PropertyCapitalizer { get; set; }

        [Parameter(HelpMessage = "Support mixed content")]
        public SwitchParameter MixedContent { get; set; }

        [Parameter(HelpMessage = "Sets the output namespace")]
        public string Namespace { get; set; } = "Xsd2";

        [Parameter(HelpMessage = "Adds imports")]
        public string[] Imports { get; set; }

        [Parameter(HelpMessage = "Sets namespaces to use")]
        public string[] Usings { get; set; }

        [Parameter(ParameterSetName = ExplicitParameterSetName, HelpMessage = "Exclude imported types")]
        public SwitchParameter ExcludeImports { get; set; }

        [Parameter(HelpMessage = "Exclude imported types by name")]
        public SwitchParameter ExcludeImportsByName { get; set; }

        [Parameter(ParameterSetName = ExplicitParameterSetName, HelpMessage = "Use nullable types")]
        public SwitchParameter UseNullable { get; set; }

        [Parameter(HelpMessage = "Combine output to a single file")]
        public string CombineTo { get; set; }

        [Parameter(ParameterSetName = AllParameterSetName, HelpMessage = "Enable common flags")]
        public SwitchParameter All { get; set; }

        [Parameter(Mandatory = true, ValueFromRemainingArguments = true)]
        public string[] Inputs { get; set; }

        private XsdCodeGeneratorOptions Options { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            Options = new XsdCodeGeneratorOptions
            {
                UseNullableTypes = UseNullable.ToBool() || All.ToBool(),
                OutputNamespace = Namespace,
                MixedContent = MixedContent.ToBool(),
                ExcludeImportedTypes = ExcludeImports.ToBool() || All.ToBool(),
                ExcludeImportedTypesByNameAndNamespace = ExcludeImportsByName.ToBool(),
                Imports = Imports?.Select(GetPath).ToList() ?? new System.Collections.Generic.List<string>(),
                UsingNamespaces = Usings?.ToList() ?? new System.Collections.Generic.List<string>(),
                Language = Language,
                WriteFileHeader = WriteHeader.ToBool(),
                PreserveOrder = PreserveOrder.ToBool(),
                EnableDataBinding = EnableDataBinding.ToBool(),
                UseLists = UseLists.ToBool() || All.ToBool(),
                UseXLinq = UseXlinq.ToBool(),
                AttributesToRemove = new System.Collections.Generic.HashSet<string>(RemoveAttributes ?? Enumerable.Empty<string>()),
                EnumValueCapitalizer = EnumCapitalizer ?? new NoneCapitalizer(),
                TypeNameCapitalizer = TypeCapitalizer ?? new NoneCapitalizer(),
                PropertyNameCapitalizer = PropertyCapitalizer ?? new NoneCapitalizer()
            };

            if (PclTarget)
            {
                Options.AttributesToRemove.Add("System.SerializableAttribute");
                Options.AttributesToRemove.Add("System.ComponentModel.DesignerCategoryAttribute");
            }

            if (StripDebugAttributes || All)
                Options.AttributesToRemove.Add("System.Diagnostics.DebuggerStepThroughAttribute");

            if (All)
            {
                if (Options.EnumValueCapitalizer is NoneCapitalizer)
                    Options.EnumValueCapitalizer = new FirstCharacterCapitalizer();
                if (Options.TypeNameCapitalizer is NoneCapitalizer)
                    Options.TypeNameCapitalizer = new FirstCharacterCapitalizer();
                if (Options.PropertyNameCapitalizer is NoneCapitalizer)
                    Options.PropertyNameCapitalizer = new FirstCharacterCapitalizer();
            }
        }

        protected override void ProcessRecord()
        {
            string outputFileExtension;

            switch (Options.Language)
            {
                case XsdCodeGeneratorOutputLanguage.VB:
                    outputFileExtension = ".vb";
                    break;
                default:
                    outputFileExtension = ".cs";
                    break;
            }

            var generator = new XsdCodeGenerator() { Options = Options };

            var inputs = Inputs.Select(GetPath).ToList();

            if (!string.IsNullOrEmpty(CombineTo))
            {
                using (var output = File.CreateText(GetPath(CombineTo)))
                    generator.Generate(inputs, output);
            }
            else
            {
                foreach (var path in inputs)
                {
                    var fileInfo = new FileInfo(path);
                    var outputPath = Path.Combine(
                        !string.IsNullOrEmpty(OutputDirectory) ? GetPath(OutputDirectory) : fileInfo.DirectoryName,
                        Path.ChangeExtension(fileInfo.Name, outputFileExtension));

                    using (var output = File.CreateText(outputPath))
                        generator.Generate(new[] { path }, output);
                }
            }
        }

        private string GetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = ".";

            return SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
        }
    }
}
