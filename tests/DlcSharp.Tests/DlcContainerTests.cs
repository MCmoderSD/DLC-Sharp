using System.Text.Json.Nodes;

namespace DlcSharp.Tests
{
    public class DlcContainerTests
    {
        private static DlcContainer Sample()
        {
            return new DlcContainer
            {
                Header = new DlcHeader
                {
                    GeneratorApp = "TestGenerator",
                    GeneratorVersion = "1.2.3",
                    GeneratorUrl = "https://generator.test",
                    TributeName = "tester",
                    DlcXmlVersion = "20_02_2008",
                },
                Packages =
                [
                    new DlcPackage
                    {
                        Name = "Müller Paket",
                        Passwords = ["geheim"],
                        Comment = "Ein Kommentar",
                        Category = "movies",
                        Files = [new DlcFile { Url = "https://host.test/ä ö ü.txt", FileName = "ä ö ü.txt", Size = 42 }],
                    },
                    new DlcPackage
                    {
                        Name = "Zweites Paket",
                        Files = [new DlcFile { Url = "https://host.test/b.bin" }],
                    },
                ],
                ContainerKey = DlcTestFactory.ContainerKey(),
            };
        }

        [Fact]
        public void NewModels_HaveEmptyListsInsteadOfNull()
        {
            var container = new DlcContainer();
            var package = new DlcPackage();

            Assert.NotNull(container.Header);
            Assert.Empty(container.Packages);
            Assert.Empty(container.ContainerKey);
            Assert.Empty(package.Passwords);
            Assert.Empty(package.Files);
        }

        [Fact]
        public void ToJson_Compact_WritesTheExpectedJson()
        {
            const string expected =
                """{"header":{"generatorApp":"TestGenerator","generatorVersion":"1.2.3","generatorUrl":"https://generator.test","tributeName":"tester","dlcXmlVersion":"20_02_2008"}"""
                + ""","packages":[{"name":"Müller Paket","passwords":["geheim"],"comment":"Ein Kommentar","category":"movies","files":[{"url":"https://host.test/ä ö ü.txt","fileName":"ä ö ü.txt","size":42}]}"""
                + """,{"name":"Zweites Paket","passwords":[],"files":[{"url":"https://host.test/b.bin"}]}]}""";

            Assert.Equal(expected, Sample().ToJson(false));
        }

        [Fact]
        public void ToJson_Indented_IsTheDefaultAndHoldsTheSameData()
        {
            var container = Sample();
            var indented = container.ToJson();

            Assert.Equal(indented, container.ToJson());
            Assert.Contains(Environment.NewLine + "  \"header\": {", indented);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(container.ToJson(false)), JsonNode.Parse(indented)));
        }

        [Fact]
        public void ToJson_OfAnEmptyContainer_WritesAnEmptyHeaderAndNoPackages()
        {
            Assert.Equal("""{"header":{},"packages":[]}""", new DlcContainer().ToJson(false));
        }
    }
}