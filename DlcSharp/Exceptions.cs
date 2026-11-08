using System.Net;

namespace DlcSharp;

public class DlcException : Exception
{
    protected DlcException(string message, Exception? innerException = null) : base(message, innerException) { }
}

public sealed class DlcFormatException : DlcException
{
    public DlcFormatException(string message, Exception? innerException = null) : base(message, innerException) { }
}

public enum DlcServiceError
{
    Unreachable,

    Timeout,

    HttpError,

    MissingKey,

    InvalidKey
}

public sealed class DlcServiceException : DlcException
{
    public DlcServiceException(DlcServiceError error, string message, HttpStatusCode? statusCode = null, Exception? innerException = null) : base(message, innerException)
    {
        Error = error;
        StatusCode = statusCode;
    }

    public DlcServiceError Error { get; }

    public HttpStatusCode? StatusCode { get; }
}