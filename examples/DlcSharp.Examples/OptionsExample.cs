using DlcSharp;

namespace Examples;

// The defaults work out of the box. Options and your own HttpClient are only needed for special cases.
internal static class OptionsExample
{
    public static async Task RunAsync(string dlcPath)
    {
        var options = new DlcDecoderOptions
        {
            // Give up if the key service does not answer within 10 seconds (the default is 30).
            // Use Timeout.InfiniteTimeSpan to wait without a limit.
            Timeout = TimeSpan.FromSeconds(10)

            // The settings below are commented out because they are rarely needed.

            // Send the request through your own proxy or mirror instead of the JDownloader service:
            // ServiceUrl = new Uri("https://my-proxy.example/dlcrypt/service.php"),

            // Use your own application credentials. These three values belong together,
            // and the key and the IV must be exactly 16 bytes each:
            // AppName = "myapp",
            // AppKey = Encoding.ASCII.GetBytes("0123456789abcdef"),
            // AppIv = Encoding.ASCII.GetBytes("fedcba9876543210"),
        };

        // You can also bring your own HttpClient, for example to use a proxy, to add logging, or to get one from
        // IHttpClientFactory in an ASP.NET app. The decoder does not dispose it.
        // The handler below prints what is sent, so you can see exactly what leaves your machine.
        using var httpClient = new HttpClient(new LoggingHandler(new HttpClientHandler()));

        // Both are optional: new DlcDecoder(), new DlcDecoder(options) and new DlcDecoder(httpClient, options) all work.
        // The options are checked and copied here. Invalid values throw an ArgumentException right away.
        var decoder = new DlcDecoder(httpClient, options);

        await using var stream = File.OpenRead(dlcPath);
        var container = await decoder.DecodeAsync(stream);

        Console.WriteLine($"Decoded with a {options.Timeout.TotalSeconds:0} second timeout: {container.Packages.Count} package(s).");
    }

    // Prints every request and its status code. The key part is shortened in the output.
    private sealed class LoggingHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            var query = uri.Query;
            var cut = query.IndexOf("data=", StringComparison.Ordinal) + "data=".Length;

            Console.WriteLine($"-> {request.Method} {uri.GetLeftPart(UriPartial.Path)}");
            Console.WriteLine($"   {query[..cut]}{query[cut..Math.Min(query.Length, cut + 10)]}... (the key part is {query.Length - cut} characters, URL-encoded)");

            var response = await base.SendAsync(request, cancellationToken);

            Console.WriteLine($"<- {(int)response.StatusCode} {response.ReasonPhrase}");
            return response;
        }
    }
}