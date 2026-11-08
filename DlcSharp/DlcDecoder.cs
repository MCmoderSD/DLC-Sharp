using System.Text;
using System.Text.RegularExpressions;
using DlcSharp.Internal;

namespace DlcSharp;

/// <summary>
/// Decrypts JDownloader DLC containers into <see cref="DlcContainer"/> objects.
/// </summary>
/// <remarks>
/// Opening a container needs one request to the JDownloader key service, see <see cref="DlcDecoderOptions"/>.
/// A decoder does not change after it is created, so one instance can be reused for many files and from several threads.
/// </remarks>
/// <example>
/// <code>
/// var decoder = new DlcDecoder();
/// await using var stream = File.OpenRead("links.dlc");
/// DlcContainer container = await decoder.DecodeAsync(stream);
///
/// foreach (DlcPackage package in container.Packages)
/// {
///     Console.WriteLine($"{package.Name}: {package.Files.Count} files");
/// }
/// </code>
/// </example>
public sealed partial class DlcDecoder
{
    [GeneratedRegex("<rc>(.*?)</rc>", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex RcElementRegex();
    
    private static readonly Regex RcPattern = RcElementRegex();
    private static readonly HttpClient SharedClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    private readonly HttpClient _httpClient;

    private readonly DlcDecoderOptions _options;

    /// <summary>
    /// Creates a decoder with the default <see cref="DlcDecoderOptions"/> and a shared <see cref="HttpClient"/>.
    /// </summary>
    public DlcDecoder()
    {
        _httpClient = SharedClient;
        _options = new DlcDecoderOptions().CloneAndValidate();
    }

    /// <summary>
    /// Creates a decoder with your own options and a shared <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="options">The settings. They are checked and copied here, so later changes to the object have no effect.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A value in <paramref name="options"/> is not valid.</exception>
    public DlcDecoder(DlcDecoderOptions options)
    {
        _httpClient = SharedClient;
        _options = (options ?? throw new ArgumentNullException(nameof(options))).CloneAndValidate();
    }

    /// <summary>
    /// Creates a decoder that sends its request with your own <see cref="HttpClient"/>,
    /// for example to use a proxy, to add logging or to get the client from <c>IHttpClientFactory</c>.
    /// </summary>
    /// <param name="httpClient">The client for the key service request. The decoder does not dispose it.</param>
    /// <param name="options">The settings, or <see langword="null"/> for the defaults. They are checked and copied here.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A value in <paramref name="options"/> is not valid.</exception>
    public DlcDecoder(HttpClient httpClient, DlcDecoderOptions? options = null)
    {   
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = (options ?? new DlcDecoderOptions()).CloneAndValidate();
    }

    /// <summary>
    /// Decrypts a container that is given as text.
    /// </summary>
    /// <param name="dlcContent">The content of a .dlc file. Whitespace, line breaks and a byte order mark are ignored.</param>
    /// <param name="cancellationToken">Cancels the request to the key service.</param>
    /// <returns>The packages and files of the container.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dlcContent"/> is <see langword="null"/>.</exception>
    /// <exception cref="DlcFormatException">The input is not a usable DLC container.</exception>
    /// <exception cref="DlcServiceException">The key service did not provide a key.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public Task<DlcContainer> DecodeAsync(string dlcContent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dlcContent);
        return DecodeTextAsync(dlcContent, cancellationToken);
    } 

    /// <summary>
    /// Decrypts a container that is given as bytes.
    /// </summary>
    /// <param name="dlcBytes">The bytes of a .dlc file, as UTF-8 text.</param>
    /// <param name="cancellationToken">Cancels the request to the key service.</param>
    /// <returns>The packages and files of the container.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dlcBytes"/> is <see langword="null"/>.</exception>
    /// <exception cref="DlcFormatException">The input is not a usable DLC container.</exception>
    /// <exception cref="DlcServiceException">The key service did not provide a key.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public Task<DlcContainer> DecodeAsync(byte[] dlcBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dlcBytes);
        return DecodeTextAsync(DlcInput.ReadText(dlcBytes), cancellationToken);
    }

    /// <summary>
    /// Decrypts a container that is read from a stream.
    /// </summary>
    /// <param name="stream">A stream with the content of a .dlc file, for example a <see cref="FileStream"/>. It is read to the end and left open.</param>
    /// <param name="cancellationToken">Cancels reading the stream and the request to the key service.</param>
    /// <returns>The packages and files of the container.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="DlcFormatException">The input is not a usable DLC container.</exception>
    /// <exception cref="DlcServiceException">The key service did not provide a key.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<DlcContainer> DecodeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var text = await DlcInput.ReadTextAsync(stream, cancellationToken).ConfigureAwait(false);
        return await DecodeTextAsync(text, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Decrypts a container without any network access, using a container key that is already known.
    /// </summary>
    /// <remarks>
    /// Take the key from <see cref="DlcContainer.ContainerKey"/> of an earlier call to one of the <c>DecodeAsync</c> methods.
    /// </remarks>
    /// <param name="dlcContent">The content of a .dlc file.</param>
    /// <param name="containerKey">The 16-byte container key of this file.</param>
    /// <returns>The packages and files of the container.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dlcContent"/> or <paramref name="containerKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="containerKey"/> is not exactly 16 bytes.</exception>
    /// <exception cref="DlcFormatException">The input is not a usable DLC container, or the key does not belong to it.</exception>
    public DlcContainer Decode(string dlcContent, byte[] containerKey)
    {
        ArgumentNullException.ThrowIfNull(dlcContent);
        ArgumentNullException.ThrowIfNull(containerKey);
        
        if (containerKey.Length != DlcCrypto.BlockSize)
        {
            throw new ArgumentException("The container key must be exactly 16 bytes.", nameof(containerKey));
        }

        return DecodePayload(DlcInput.Parse(dlcContent).Payload, containerKey);
    }

    private async Task<DlcContainer> DecodeTextAsync(string text, CancellationToken cancellationToken)
    {
        var input = DlcInput.Parse(text);
        var containerKey = await FetchContainerKeyAsync(input.KeyPart, cancellationToken).ConfigureAwait(false);
        return DecodePayload(input.Payload, containerKey);
    }

    private static DlcContainer DecodePayload(string payload, byte[] containerKey)
    {
        var encrypted = LenientBase64.Decode(payload);
        if (encrypted.Length == 0 || encrypted.Length % DlcCrypto.BlockSize != 0)
        {
            throw new DlcFormatException($"The encrypted payload is truncated or corrupt: its length ({encrypted.Length} bytes) is not a positive multiple of 16.");
        }

        var decrypted = DlcCrypto.DecryptCbcNoPadding(encrypted, containerKey, containerKey);

        var xmlBytes = LenientBase64.Decode(Encoding.ASCII.GetString(decrypted));
        var xml = Encoding.UTF8.GetString(xmlBytes).Trim('\uFEFF', '\0', ' ', '\t', '\r', '\n');
        if (!xml.StartsWith('<'))
        {
            throw new DlcFormatException("The decrypted payload is not XML. The container key is probably wrong: the key service may have rate-limited this IP address or blocked the application name, or the input is not a DLC container.");
        }

        return DlcXmlParser.Parse(xml, containerKey);
    }

    private async Task<byte[]> FetchContainerKeyAsync(string keyPart, CancellationToken cancellationToken)
    {
        string body;
        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            if (_options.Timeout != Timeout.InfiniteTimeSpan)
            {
                linked.CancelAfter(_options.Timeout);
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, BuildRequestUri(keyPart));

                using var response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token)
                    .ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new DlcServiceException(
                        DlcServiceError.HttpError,
                        $"The key service answered with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).",
                        response.StatusCode);
                }

                body = await response.Content.ReadAsStringAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new DlcServiceException(
                    DlcServiceError.Timeout,
                    $"The key service did not answer within {_options.Timeout}.",
                    innerException: ex);
            }
            catch (HttpRequestException ex)
            {
                throw new DlcServiceException(
                    DlcServiceError.Unreachable,
                    $"The key service could not be reached: {ex.Message}",
                    innerException: ex);
            }
        }

        var match = RcPattern.Match(body);
        if (!match.Success)
        {
            throw new DlcServiceException(DlcServiceError.MissingKey, "The key service response contains no rc element. The service may have rate-limited this IP address or blocked the application name.");
        }

        var wrapped = LenientBase64.Decode(match.Groups[1].Value.Trim());
        if (wrapped.Length < DlcCrypto.BlockSize)
        {
            throw new DlcServiceException(DlcServiceError.InvalidKey, $"The key service returned a key of only {wrapped.Length} bytes; 16 are required.");
        }

        var block = new byte[DlcCrypto.BlockSize];
        Buffer.BlockCopy(wrapped, 0, block, 0, block.Length);
        return DlcCrypto.DecryptCbcNoPadding(block, _options.AppKey, _options.AppIv);
    }

    private Uri BuildRequestUri(string keyPart)
    {
        var baseUrl = _options.ServiceUrl.AbsoluteUri;
        var separator = baseUrl.Contains('?') ? '&' : '?';
        return new Uri($"{baseUrl}{separator}srcType=dlc&destType={Uri.EscapeDataString(_options.AppName)}&data={Uri.EscapeDataString(keyPart)}");
    }
}