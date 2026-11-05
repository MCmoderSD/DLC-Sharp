# DlcSharp examples

Small, runnable examples that show how to use the library. Each one is a single short file in
[DlcSharp.Examples](DlcSharp.Examples). Run them from the repository root with a `.dlc` file of your own:

```
dotnet run --project examples/DlcSharp.Examples -- <example> <file.dlc>
```

| Example | What it shows | File |
| --- | --- | --- |
| `basic` | Open a file, list packages, files, sizes and links, and turn the result into JSON. Also shows the text and bytes forms of the input | [BasicExample.cs](DlcSharp.Examples/BasicExample.cs) |
| `offline` | Save the container key once, then open the same file without network | [OfflineExample.cs](DlcSharp.Examples/OfflineExample.cs) |
| `options` | Change the timeout, address or application credentials, and bring your own `HttpClient` that prints what is sent | [OptionsExample.cs](DlcSharp.Examples/OptionsExample.cs) |
| `errors` | Handle a broken file, a failing key service and cancelling | [ErrorHandlingExample.cs](DlcSharp.Examples/ErrorHandlingExample.cs) |

## Good to know

- Opening a `.dlc` file sends its last 88 characters (the key part) to the JDownloader key service. The `options` example prints exactly what is sent.
- The `offline` example writes the container key next to your file as `<file>.dlc.key`. Treat it like the file itself: together they unlock the content.
