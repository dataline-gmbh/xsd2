using System.IO;
using System.Xml.Serialization;
using Xunit;

namespace Xsd2.Tests
{
    public class ParseTests
    {
        [Fact]
        public void NullableFieldsAreRead()
        {
            var form = ParseForm(@"schemas\form1.xml");
            Assert.Equal(2, form.Items.Items.Count);
            Assert.Equal(1, form.Items.Items[0].MinLength);
            Assert.Null(form.Items.Items[1].MinLength);
        }

        private Form ParseForm(string path)
        {
            var xmlSerializer = new XmlSerializer(typeof(Form));
            using (var x = File.OpenRead(path))
                return (Form)xmlSerializer.Deserialize(x);
        }
    }
}
