using System.Text;

namespace DlcSharp;

/// <summary>
/// Settings of a <see cref="DlcDecoder"/>: where the key service is, which application credentials are sent to it and how long to wait.
/// </summary>
/// <remarks>
/// The defaults use the JDownloader service with the application name, key and IV of the download manager pyLoad.
/// They are not an official release from JDownloader or AppWork.
/// A decoder checks and copies the options when it is created, and throws an <see cref="ArgumentException"/> for a value that is not valid.
/// </remarks>
public sealed class DlcDecoderOptions
{
    private const string DefaultServiceUrl = "https://service.jdownloader.org/dlcrypt/service.php";
    private const string DefaultAppName = "pylo";               // AppName from pyLoad
    private const string DefaultAppKey = "cb99b5cbc24db398";    // AppKey from pyLoad
    private const string DefaultAppIv = "9bc24cb995cb8db3";     // AppIv from pyLoad

    /// <summary>
    /// Address of the key service, an absolute http or https URL. Default: the JDownloader service over HTTPS.
    /// </summary>
    public Uri ServiceUrl { get; set; } = new(DefaultServiceUrl);

    /// <summary>
    /// Application name, sent to the key service as <c>destType</c>. Must not be empty. Default: <c>pylo</c>.
    /// </summary>
    public string AppName { get; set; } = DefaultAppName;

    /// <summary>
    /// The 16-byte AES key that unwraps the container key from the answer of the service.
    /// It belongs together with <see cref="AppName"/> and <see cref="AppIv"/>.
    /// </summary>
    public byte[] AppKey { get; set; } = Encoding.ASCII.GetBytes(DefaultAppKey);

    /// <summary>
    /// The 16-byte initialization vector that goes with <see cref="AppKey"/>.
    /// </summary>
    public byte[] AppIv { get; set; } = Encoding.ASCII.GetBytes(DefaultAppIv);

    /// <summary>
    /// How long to wait for the key service. Must be positive, or <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to wait without a limit.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    internal DlcDecoderOptions CloneAndValidate()
    {
        if (ServiceUrl == null || !ServiceUrl.IsAbsoluteUri || (ServiceUrl.Scheme != Uri.UriSchemeHttp && ServiceUrl.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("ServiceUrl must be an absolute http or https URL.", nameof(ServiceUrl));
        }

        if (string.IsNullOrWhiteSpace(AppName))
        {
            throw new ArgumentException("AppName must not be empty.", nameof(AppName));
        }

        if (AppKey is not { Length: 16 })
        {
            throw new ArgumentException("AppKey must be exactly 16 bytes.", nameof(AppKey));
        }

        if (AppIv is not { Length: 16 })
        {
            throw new ArgumentException("AppIv must be exactly 16 bytes.", nameof(AppIv));
        }

        if (Timeout <= TimeSpan.Zero && Timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentException("Timeout must be positive or Timeout.InfiniteTimeSpan.", nameof(Timeout));
        }

        return new DlcDecoderOptions
        {
            ServiceUrl = ServiceUrl,
            AppName = AppName,
            AppKey = (byte[]) AppKey.Clone(),
            AppIv = (byte[]) AppIv.Clone(),
            Timeout = Timeout
        };
    }
}