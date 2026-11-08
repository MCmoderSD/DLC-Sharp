using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DlcSharp.Internal
{
    internal static class DlcXmlParser
    {
        public static DlcContainer Parse(string xml, byte[] containerKey)
        {
            XDocument document;
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                };

                using var stringReader = new StringReader(xml);
                using var reader = XmlReader.Create(stringReader, settings);
                document = XDocument.Load(reader);
            }
            catch (XmlException ex)
            {
                throw new DlcFormatException("The decrypted container is not valid XML: " + ex.Message, ex);
            }

            var root = document.Root!;
            var content = root.Element("content");
            if (content == null)
            {
                throw new DlcFormatException("The decrypted XML has no content element, so it is not a DLC container.");
            }

            var packages = content.Elements("package").Select(ParsePackage).ToList();
            var header = root.Element("header");
            var generator = header?.Element("generator");

            return new DlcContainer
            {
                Header = new DlcHeader
                {
                    GeneratorApp = Field(generator?.Element("app")),
                    GeneratorVersion = Field(generator?.Element("version")),
                    GeneratorUrl = Field(generator?.Element("url")),
                    TributeName = Field(header?.Element("tribute")?.Element("name")),
                    DlcXmlVersion = Field(header?.Element("dlcxmlversion")),
                },
                Packages = packages,
                ContainerKey = (byte[]) containerKey.Clone(),
            };
        }

        private static DlcPackage ParsePackage(XElement package)
        {
            return new DlcPackage
            {
                Name = Attribute(package, "name"),
                Passwords = ParsePasswords(Attribute(package, "passwords")),
                Comment = Attribute(package, "comment"),
                Category = Attribute(package, "category"),
                Files = ParseFiles(package),
            };
        }

        private static List<DlcFile> ParseFiles(XElement package)
        {
            var files = new List<DlcFile>();
            foreach (var file in package.Elements("file"))
            {
                files.Add(new DlcFile
                {
                    Url = Field(file.Element("url")),
                    FileName = Field(file.Element("filename")),
                    Size = ParseSize(Field(file.Element("size"))),
                });
            }
            return files;
        }

        private static string? Field(XElement? element)
        {
            return Normalize(element?.Value);
        }

        private static string? Attribute(XElement element, string name)
        {
            return Normalize(element.Attribute(name)?.Value);
        }

        private static string? Normalize(string? raw)
        {
            if (raw == null)
            {
                return null;
            }

            var decoded = LenientBase64.DecodeFieldOrRaw(raw);
            return decoded.Length == 0 ? null : decoded;
        }

        private static string[] ParsePasswords(string? text)
        {
            if (text == null)
            {
                return [];
            }

            text = text.Trim();
            if (text.Length == 0)
            {
                return [];
            }

            if (text.Length >= 2 && ((text[0] == '{' && text[^1] == '}') || (text[0] == '[' && text[^1] == ']')))
            {
                return SplitPasswordList(text[1..^1]);
            }

            if (text is ['"' or '\'', _, ..] && text[^1] == text[0])
            {
                return SplitPasswordList(text);
            }

            return [text];
        }

        private static string[] SplitPasswordList(string text)
        {
            var items = new List<string>();
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c) || c == ',')
                {
                    i++;
                    continue;
                }

                var item = new StringBuilder();
                if (c is '"' or '\'')
                {
                    i++;
                    while (i < text.Length && text[i] != c)
                    {
                        if (text[i] == '\\' && i + 1 < text.Length && (text[i + 1] == c || text[i + 1] == '\\'))
                        {
                            i++;
                        }

                        item.Append(text[i]);
                        i++;
                    }

                    i++;
                }
                else
                {
                    while (i < text.Length && text[i] != ',')
                    {
                        item.Append(text[i]);
                        i++;
                    }
                }

                var value = c is '"' or '\'' ? item.ToString() : item.ToString().Trim();
                if (value.Length > 0)
                {
                    items.Add(value);
                }
            }

            return [.. items];
        }

        private static long? ParseSize(string? text)
        {
            if (text != null && long.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var size))
            {
                return size;
            }

            return null;
        }
    }
}