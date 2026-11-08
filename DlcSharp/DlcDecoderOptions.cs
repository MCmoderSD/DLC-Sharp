using System.Text;

namespace DlcSharp;

public sealed class DlcDecoderOptions
{
    private const string DefaultServiceUrl = "https://service.jdownloader.org/dlcrypt/service.php";
    private const string DefaultAppName = "pylo";               // AppName from pyLoad
    private const string DefaultAppKey = "cb99b5cbc24db398";    // AppKey from pyLoad
    private const string DefaultAppIv = "9bc24cb995cb8db3";     // AppIv from pyLoad

    public Uri ServiceUrl { get; set; } = new(DefaultServiceUrl);

    public string AppName { get; set; } = DefaultAppName;

    public byte[] AppKey { get; set; } = Encoding.ASCII.GetBytes(DefaultAppKey);

    public byte[] AppIv { get; set; } = Encoding.ASCII.GetBytes(DefaultAppIv);

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