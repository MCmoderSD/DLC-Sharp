using System.Net;

namespace DlcSharp.Tests;

public class ExceptionTests
{
    [Fact]
    public void DlcFormatException_KeepsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("cause");
        var exception = new DlcFormatException("broken", inner);

        Assert.Equal("broken", exception.Message);
        Assert.Same(inner, exception.InnerException);
        Assert.Null(new DlcFormatException("broken").InnerException);
    }

    [Fact]
    public void DlcServiceException_KeepsAllValues()
    {
        var inner = new HttpRequestException("cause");
        var exception = new DlcServiceException(DlcServiceError.HttpError, "failed", HttpStatusCode.BadGateway, inner);

        Assert.Equal(DlcServiceError.HttpError, exception.Error);
        Assert.Equal("failed", exception.Message);
        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void DlcServiceException_WithoutOptionalValues_LeavesThemEmpty()
    {
        var exception = new DlcServiceException(DlcServiceError.MissingKey, "no key");

        Assert.Null(exception.StatusCode);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void BothExceptions_DeriveFromDlcException()
    {
        Assert.IsType<DlcException>(new DlcFormatException("x"), exactMatch: false);
        Assert.IsType<DlcException>(new DlcServiceException(DlcServiceError.Unreachable, "x"), exactMatch: false);
    }
}