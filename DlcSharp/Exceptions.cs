using System.Net;

namespace DlcSharp;

/// <summary>
/// Base class of the exceptions that DlcSharp throws on purpose, so a single <c>catch</c> handles all of them.
/// </summary>
/// <remarks>
/// The class is <c>closed</c>: only <see cref="DlcFormatException"/> and <see cref="DlcServiceException"/> derive from it,
/// so the compiler can check that a <c>switch</c> over a <see cref="DlcException"/> covers both.
/// </remarks>
public closed class DlcException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The error that caused this one, if there is one.</param>
    private protected DlcException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// The input is not a usable DLC container: it is too short or not Base64, its content is corrupt,
/// the container key does not fit, or the decrypted XML is malformed.
/// </summary>
public sealed class DlcFormatException : DlcException
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">What is wrong with the input.</param>
    /// <param name="innerException">The error that caused this one, if there is one.</param>
    public DlcFormatException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Why the key service did not provide a key.
/// </summary>
public enum DlcServiceError
{
    /// <summary>The service could not be reached, for example because of a network error.</summary>
    Unreachable,

    /// <summary>The service did not answer within <see cref="DlcDecoderOptions.Timeout"/>.</summary>
    Timeout,

    /// <summary>The service answered with an HTTP error status. See <see cref="DlcServiceException.StatusCode"/>.</summary>
    HttpError,

    /// <summary>The answer contained no key, for example because of a rate limit or a blocked application name.</summary>
    MissingKey,

    /// <summary>The key in the answer was too short to be used.</summary>
    InvalidKey
}

/// <summary>
/// The JDownloader key service did not provide a key. <see cref="Error"/> says why.
/// </summary>
public sealed class DlcServiceException : DlcException
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="error">Why the service did not provide a key.</param>
    /// <param name="message">What went wrong.</param>
    /// <param name="statusCode">The HTTP status code, if the service answered with one.</param>
    /// <param name="innerException">The error that caused this one, if there is one.</param>
    public DlcServiceException(DlcServiceError error, string message, HttpStatusCode? statusCode = null, Exception? innerException = null) : base(message, innerException)
    {
        Error = error;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Why the service did not provide a key.
    /// </summary>
    public DlcServiceError Error { get; }

    /// <summary>
    /// The HTTP status code of the answer. Only set for <see cref="DlcServiceError.HttpError"/>, otherwise <see langword="null"/>.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }
}