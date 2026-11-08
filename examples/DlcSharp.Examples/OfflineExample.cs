using DlcSharp;

namespace Examples;

// Opening a file needs the key service, but only once. The container key it returns can be saved,
// and the same file can then be opened again without any network access.
// Run this example twice: the first run saves the key, the second run uses it.
internal static class OfflineExample
{
    public static async Task RunAsync(string dlcPath)
    {
        var content = await File.ReadAllTextAsync(dlcPath);
        var keyPath = dlcPath + ".key";

        if (File.Exists(keyPath))
        {
            // Second run: the key is known. Decode is static, so it needs no decoder and sends no request.
            var key = Convert.FromBase64String(await File.ReadAllTextAsync(keyPath));
            var container = DlcDecoder.Decode(content, key);

            Console.WriteLine($"Decoded offline with the saved key: {container.Packages.Count} package(s).");
            Console.WriteLine($"Delete '{keyPath}' to try the online path again.");
        }
        else
        {
            // First run: decode online and keep the 16-byte container key.
            var decoder = new DlcDecoder();
            var container = await decoder.DecodeAsync(content);
            await File.WriteAllTextAsync(keyPath, Convert.ToBase64String(container.ContainerKey));

            Console.WriteLine($"Decoded online: {container.Packages.Count} package(s).");
            Console.WriteLine($"Saved the container key to '{keyPath}'. Run this example again to decode offline.");
            Console.WriteLine("Keep the key as private as the file itself: together they unlock the content.");
        }
    }
}