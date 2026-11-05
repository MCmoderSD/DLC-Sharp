using DlcSharp;

namespace Examples
{
    // The simplest use: open a .dlc file, list what is inside and turn the result into JSON.
    internal static class BasicExample
    {
        public static async Task RunAsync(string dlcPath)
        {
            // One decoder can be created once and reused for many files, also from several threads.
            var decoder = new DlcDecoder();

            // The library does not read files itself. Open the file and pass the stream (it stays open).
            await using var stream = File.OpenRead(dlcPath);
            var container = await decoder.DecodeAsync(stream);

            // The same content can also be passed as text or as bytes:
            //   var container = await decoder.DecodeAsync(await File.ReadAllTextAsync(dlcPath));
            //   var container = await decoder.DecodeAsync(await File.ReadAllBytesAsync(dlcPath));

            // The header tells which program created the container. Every value can be null.
            Console.WriteLine($"Created by: {container.Header.GeneratorApp ?? "(not stated)"}");
            Console.WriteLine($"Packages:   {container.Packages.Count}");

            foreach (var package in container.Packages)
            {
                Console.WriteLine();
                Console.WriteLine($"Package:   {package.Name}");
                Console.WriteLine($"Category:  {package.Category ?? "-"}");
                Console.WriteLine($"Comment:   {package.Comment ?? "-"}");

                // Passwords are never null. An empty array means: no passwords.
                Console.WriteLine($"Passwords: {(package.Passwords.Length == 0 ? "-" : string.Join(", ", package.Passwords))}");
                Console.WriteLine($"Files:     {package.Files.Count}");

                // Only the first five files are printed to keep the output short.
                foreach (var file in package.Files.Take(5))
                {
                    Console.WriteLine($"  {file.FileName} ({file.Size} bytes)");
                    Console.WriteLine($"    {file.Url}");
                }

                if (package.Files.Count > 5)
                {
                    Console.WriteLine($"  ... and {package.Files.Count - 5} more");
                }
            }

            // The whole result can be written out as JSON with one call.
            // Names are camelCase, values that are null are left out, and the container key is never included.
            var json = container.ToJson();                      // readable, with indentation (default)
            var compact = container.ToJson(indented: false);    // one line, for example for a log

            Console.WriteLine();
            Console.WriteLine($"As JSON ({compact.Length} characters compact), first lines:");
            foreach (var line in json.Split('\n').Take(10))
            {
                Console.WriteLine(line.TrimEnd('\r'));
            }

            Console.WriteLine("...");

            // To save it
            await File.WriteAllTextAsync("links.json", json);
        }
    }
}