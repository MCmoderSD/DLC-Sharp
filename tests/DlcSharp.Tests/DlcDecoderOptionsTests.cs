namespace DlcSharp.Tests;

public class DlcDecoderOptionsTests
{
    [Fact]
    public void DefaultsPointToTheJDownloaderServiceWithThePyLoadApplication()
    {
        var options = new DlcDecoderOptions();

        Assert.Equal("https://service.jdownloader.org/dlcrypt/service.php", options.ServiceUrl.AbsoluteUri);
        Assert.Equal("pylo", options.AppName);
        Assert.Equal("cb99b5cbc24db398"u8.ToArray(), options.AppKey);
        Assert.Equal("9bc24cb995cb8db3"u8.ToArray(), options.AppIv);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
    }

    [Theory]
    [InlineData(null, UriKind.Absolute)]
    [InlineData("/dlcrypt/service.php", UriKind.Relative)]
    [InlineData("ftp://service.test/key", UriKind.Absolute)]
    public void ConstructorRejectsAServiceUrlThatIsNotAbsoluteHttpOrHttps(string? url, UriKind kind)
    {
        var options = new DlcDecoderOptions { ServiceUrl = url == null ? null! : new Uri(url, kind) };
        var exception = Assert.Throws<ArgumentException>(() => new DlcDecoder(options));

        Assert.Equal("ServiceUrl", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void ConstructorRejectsAnEmptyAppName(string? appName)
    {
        var options = new DlcDecoderOptions { AppName = appName! };
        var exception = Assert.Throws<ArgumentException>(() => new DlcDecoder(options));

        Assert.Equal("AppName", exception.ParamName);
    }

    [Theory]
    [InlineData("AppKey", null)]
    [InlineData("AppKey", 15)]
    [InlineData("AppKey", 17)]
    [InlineData("AppIv", null)]
    [InlineData("AppIv", 15)]
    [InlineData("AppIv", 17)]
    public void ConstructorRejectsAKeyOrIvThatIsNotSixteenBytes(string property, int? length)
    {
        var value = length == null ? null! : new byte[length.Value];
        var options = property == "AppKey" ? new DlcDecoderOptions { AppKey = value } : new DlcDecoderOptions { AppIv = value };
        var exception = Assert.Throws<ArgumentException>(() => new DlcDecoder(options));

        Assert.Equal(property, exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5000)]
    public void ConstructorRejectsATimeoutThatIsNotPositive(int milliseconds)
    {
        var options = new DlcDecoderOptions { Timeout = TimeSpan.FromMilliseconds(milliseconds) };
        var exception = Assert.Throws<ArgumentException>(() => new DlcDecoder(options));

        Assert.Equal("Timeout", exception.ParamName);
    }
}