using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Xsd2.Capitalizers;

namespace Xsd2.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Capitalizer")]
    public class GetCapitalizerCmdlet : PSCmdlet
    {
        public enum CapitalizerType
        {
            None,
            FirstCharacter,
            Word
        }

        [Parameter(Mandatory = true, Position = 0)]
        public CapitalizerType Type { get; set; }

        [Parameter(Position = 1)]
        public object Parameter { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(GetCapitalizer());
        }

        private ICapitalizer GetCapitalizer()
        {
            switch (Type)
            {
                case CapitalizerType.None:
                    return new NoneCapitalizer();
                case CapitalizerType.FirstCharacter:
                    return new FirstCharacterCapitalizer();
                case CapitalizerType.Word:
                    if (Parameter is int i)
                        return new WordCapitalizer(i);
                    return new WordCapitalizer();
                default:
                    throw new NotSupportedException("This capitalizer type is not supported.");
            }
        }
    }
}
