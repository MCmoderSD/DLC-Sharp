using DlcSharp;

namespace Examples
{
    // Two kinds of errors can happen: the file is not usable, or the key service does not help.
    // Both derive from DlcException, so a single catch (DlcException) also works if you do not need the difference.
    internal static class ErrorHandlingExample
    {
        public static async Task RunAsync(string dlcPath)
        {
            var decoder = new DlcDecoder();

            // 1) Input that is clearly not a DLC file is rejected before any request is sent.
            try
            {
                await decoder.DecodeAsync("this is not a DLC container");
            }
            catch (DlcFormatException ex)
            {
                Console.WriteLine($"1) Not a DLC file: {ex.Message}");
            }

            // 2) The given file, with every kind of failure handled.
            try
            {
                await using var stream = File.OpenRead(dlcPath);
                var container = await decoder.DecodeAsync(stream);
                Console.WriteLine($"2) Decoded {container.Packages.Count} package(s).");
            }
            catch (DlcServiceException ex)
            {
                // The key service could not provide the key. ex.Error tells why.
                var advice = ex.Error switch
                {
                    DlcServiceError.Unreachable => "No connection. Check the network or the ServiceUrl.",
                    DlcServiceError.Timeout => "The service was too slow. Raise DlcDecoderOptions.Timeout or try again later.",
                    DlcServiceError.HttpError => $"The service answered with HTTP {(int?)ex.StatusCode}.",
                    DlcServiceError.MissingKey => "The answer contained no key. A rate limit or a blocked application name are possible reasons.",
                    DlcServiceError.InvalidKey => "The key in the answer was unusable.",
                    _ => "Unknown problem with the key service.",
                };
                Console.WriteLine($"2) Key service problem: {advice}");
            }
            catch (DlcFormatException ex)
            {
                // The file itself is broken, or the key did not fit.
                Console.WriteLine($"2) The file is not usable: {ex.Message}");
            }

            // 3) Cancelling. A CancellationToken stops the decoding. This is reported as
            // OperationCanceledException and is different from the Timeout option, which gives a DlcServiceException.
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
            try
            {
                await using var stream = File.OpenRead(dlcPath);
                await decoder.DecodeAsync(stream, cts.Token);
                Console.WriteLine("3) Finished before the cancellation took effect.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("3) Canceled through the CancellationToken.");
            }
            catch (DlcException ex)
            {
                Console.WriteLine($"3) Another problem: {ex.Message}");
            }
        }
    }
}