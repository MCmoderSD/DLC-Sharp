using System.Xml;
using DlcSharp.Internal;

namespace DlcSharp.Tests;

public class DlcXmlParserTests
{
    private static readonly byte[] Key = DlcTestFactory.ContainerKey();

    private static DlcContainer Parse(string xml)
    {
        return DlcXmlParser.Parse(xml, Key);
    }

    private static DlcPackage ParseSinglePackage(string packageXml)
    {
        return Assert.Single(Parse(DlcTestFactory.ContainerXml(packageXml)).Packages);
    }

    [Fact]
    public void ParseReadsEveryFieldOfTheSample()
    {
        var container = Parse(DlcTestFactory.SampleXml());

        Assert.Equal("TestGenerator", container.Header.GeneratorApp);
        Assert.Equal("1.2.3", container.Header.GeneratorVersion);
        Assert.Equal("https://generator.test", container.Header.GeneratorUrl);
        Assert.Equal("tester", container.Header.TributeName);
        Assert.Equal("20_02_2008", container.Header.DlcXmlVersion);
        Assert.Equal(2, container.Packages.Count);

        var first = container.Packages[0];
        Assert.Equal("Müller Paket", first.Name);
        Assert.Equal(["geheim"], first.Passwords);
        Assert.Equal("Ein Kommentar", first.Comment);
        Assert.Equal("movies", first.Category);
        Assert.Equal(2, first.Files.Count);
        Assert.Equal("https://host.test/a.part1.rar", first.Files[0].Url);
        Assert.Equal("a.part1.rar", first.Files[0].FileName);
        Assert.Equal(1234567890123, first.Files[0].Size);
        Assert.Equal("https://host.test/ä ö ü.txt", first.Files[1].Url);
        Assert.Equal("ä ö ü.txt", first.Files[1].FileName);
        Assert.Equal(42, first.Files[1].Size);

        var second = container.Packages[1];
        Assert.Equal("Zweites Paket", second.Name);
        Assert.Empty(second.Passwords);
        Assert.Null(second.Comment);
        Assert.Null(second.Category);
        
        var file = Assert.Single(second.Files);
        Assert.Equal("https://host.test/b.bin", file.Url);
        Assert.Equal("b.bin", file.FileName);
        Assert.Null(file.Size);
    }

    [Fact]
    public void ParseReturnsACopyOfTheContainerKey()
    {
        var container = Parse(DlcTestFactory.SampleXml());

        Assert.Equal(Key, container.ContainerKey);
        Assert.NotSame(Key, container.ContainerKey);
    }

    [Fact]
    public void ParseWithoutPackagesReturnsAnEmptyList()
    {
        Assert.Empty(Parse(DlcTestFactory.ContainerXml(string.Empty)).Packages);
    }

    [Fact]
    public void ParseIgnoresPackagesOutsideTheContentElement()
    {
        Assert.Empty(Parse("<dlc><package name=\"x\"/><content/></dlc>").Packages);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<header/>")]
    [InlineData("<header><generator/><tribute/></header>")]
    [InlineData("<header><dlcxmlversion>   </dlcxmlversion></header>")]
    public void ParseWithoutHeaderValuesLeavesTheHeaderEmpty(string header)
    {
        var result = Parse("<dlc>" + header + "<content/></dlc>").Header;

        Assert.Null(result.GeneratorApp);
        Assert.Null(result.GeneratorVersion);
        Assert.Null(result.GeneratorUrl);
        Assert.Null(result.TributeName);
        Assert.Null(result.DlcXmlVersion);
    }

    [Theory]
    [InlineData("<package/>")]
    [InlineData("<package name=\"\" passwords=\"\" comment=\"\" category=\"\"/>")]
    public void ParsePackageWithoutValuesHasNoValues(string packageXml)
    {
        var package = ParseSinglePackage(packageXml);

        Assert.Null(package.Name);
        Assert.Empty(package.Passwords);
        Assert.Null(package.Comment);
        Assert.Null(package.Category);
        Assert.Empty(package.Files);
    }

    [Fact]
    public void ParseKeepsValuesThatAreNotBase64AsTheyAre()
    {
        var package = ParseSinglePackage(
            "<package name=\"Plain Name\" passwords=\"{x, y}\" comment=\"a &amp; b\">"
            + "<file><url>http://a.test/f?x=1</url><filename>f.txt</filename><size> 42 </size></file>"
            + "</package>");

        Assert.Equal("Plain Name", package.Name);
        Assert.Equal(["x", "y"], package.Passwords);
        Assert.Equal("a & b", package.Comment);
        
        var file = Assert.Single(package.Files);
        
        Assert.Equal("http://a.test/f?x=1", file.Url);
        Assert.Equal("f.txt", file.FileName);
        Assert.Equal(42, file.Size);
    }

    [Fact]
    public void ParseKeepsAFileWithoutAUrl()
    {
        var file = Assert.Single(ParseSinglePackage(DlcTestFactory.PackageXml(null, null, null, null, DlcTestFactory.FileXml(null, "only-a-name.bin", 7))).Files);

        Assert.Null(file.Url);
        Assert.Equal("only-a-name.bin", file.FileName);
        Assert.Equal(7, file.Size);
    }

    [Theory]
    [InlineData("42", 42L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("999999999999999999999", null)]
    [InlineData("-5", null)]
    [InlineData("4.2", null)]
    [InlineData("", null)]
    public void ParseReadsTheFileSizeAsAnUnsignedNumber(string text, long? expected)
    {
        var package = ParseSinglePackage($"<package><file><size>{text}</size></file></package>");

        Assert.Equal(expected, Assert.Single(package.Files).Size);
    }

    [Theory]
    [InlineData("two words", "two words")]
    [InlineData("  padded  ", "padded")]
    [InlineData("   ", "")]
    // brace and bracket lists
    [InlineData("{ a , b }", "a|b")]
    [InlineData("[a,b]", "a|b")]
    [InlineData("{,,a,,b,}", "a|b")]
    [InlineData("{}", "")]
    [InlineData("{ab\"c, d}", "ab\"c|d")]
    // quoted items
    [InlineData("{\"a,b\", 'c d'}", "a,b|c d")]
    [InlineData("{\"\", a}", "a")]
    [InlineData("{'a\"b'}", "a\"b")]
    [InlineData("{\"abc}", "abc")]
    // escapes inside quotes
    [InlineData("{\"a\\\"b\"}", "a\"b")]
    [InlineData("{\"a\\\\b\"}", "a\\b")]
    [InlineData("{\"a\\nb\"}", "a\\nb")]
    [InlineData("{\"a\\'b\"}", "a\\'b")]
    [InlineData("{\"abc\\}", "abc\\")]
    // a whole value in quotes
    [InlineData("\"abc\"", "abc")]
    [InlineData("'abc'", "abc")]
    // things that only look like lists or quotes
    [InlineData("\"ab", "\"ab")]
    [InlineData("'ab\"", "'ab\"")]
    [InlineData("\"", "\"")]
    [InlineData("{", "{")]
    [InlineData("{abc", "{abc")]
    [InlineData("{a]", "{a]")]
    [InlineData("[a}", "[a}")]
    public void ParseReadsPasswordsInAllTheirNotations(string passwords, string expectedJoined)
    {
        var package = ParseSinglePackage(DlcTestFactory.PackageXml("p", passwords, null, null));

        var expected = expectedJoined.Length == 0 ? [] : expectedJoined.Split('|');
        Assert.Equal(expected, package.Passwords);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<dlc><content></dlc>")]
    [InlineData("not XML at all")]
    public void ParseRejectsInputThatIsNotWellFormedXml(string xml)
    {
        var exception = Assert.Throws<DlcFormatException>(() => Parse(xml));

        Assert.IsType<XmlException>(exception.InnerException);
        Assert.Contains("not valid XML", exception.Message);
    }

    [Theory]
    [InlineData("<!DOCTYPE dlc [<!ENTITY x \"y\">]><dlc><content/></dlc>")]
    [InlineData("<!DOCTYPE dlc [<!ENTITY xxe SYSTEM \"file:///c:/windows/win.ini\">]><dlc><content>&xxe;</content></dlc>")]
    [InlineData("<!DOCTYPE dlc SYSTEM \"http://127.0.0.1:9/evil.dtd\"><dlc><content/></dlc>")]
    public void ParseRejectsDocumentTypeDeclarations(string xml)
    {
        var exception = Assert.Throws<DlcFormatException>(() => Parse(xml));

        Assert.IsType<XmlException>(exception.InnerException);
    }

    [Theory]
    [InlineData("<dlc/>")]
    [InlineData("<dlc><package name=\"x\"/></dlc>")]
    public void ParseRejectsXmlWithoutAContentElement(string xml)
    {
        var exception = Assert.Throws<DlcFormatException>(() => Parse(xml));

        Assert.Contains("no content element", exception.Message);
        Assert.Null(exception.InnerException);
    }
}