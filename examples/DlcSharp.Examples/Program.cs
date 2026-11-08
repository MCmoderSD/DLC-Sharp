namespace Examples;

internal static class Program
{
    // Every example gets the path of a .dlc file.
    private static readonly Dictionary<string, (string Description, Func<string, Task> Run)> Examples = new()
    {
        ["basic"] = ("Open a .dlc file, list its packages and files, and turn the result into JSON", BasicExample.RunAsync),
        ["offline"] = ("Save the container key, then open the same file again without network", OfflineExample.RunAsync),
        ["options"] = ("Change the settings and bring your own HttpClient", OptionsExample.RunAsync),
        ["errors"] = ("Handle errors and cancellation", ErrorHandlingExample.RunAsync)
    };

    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 2 || !Examples.TryGetValue(args[0], out var example))
        {
            PrintUsage();
            return 1;
        }

        if (!File.Exists(args[1]))
        {
            Console.WriteLine($"File not found: {args[1]}");
            return 1;
        }

        await example.Run(args[1]);
        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet run --project examples/DlcSharp.Examples -- <example> <file.dlc>");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        foreach (var (name, (description, _)) in Examples)
        {
            Console.WriteLine($"  {name,-9}{description}");
        }

        Console.WriteLine();
        Console.WriteLine("Note: opening a .dlc file sends its last 88 characters (the key part) to the JDownloader key service.");
    }
}