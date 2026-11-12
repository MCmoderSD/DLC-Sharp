# DlcSharp

A small .NET library that opens **JDownloader DLC containers**. You give it a `.dlc` file and get back the packages, file names, sizes and download links as plain objects, or as JSON.

- Targets `net11.0` and has no third-party dependencies: AES comes from `System.Security.Cryptography`, JSON from `System.Text.Json`.
- Plain model classes. The JSON output uses serializers generated at compile time, so it needs no reflection.
- Written from scratch against the public format, without code from other DLC libraries.

> **Not affiliated with JDownloader or AppWork.** See [Known limits](#known-limits).

## Installation

```bash
dotnet add package DlcSharp
```

The package targets `net11.0`. On .NET 10, use version 1.0.0.

## Quick start

```csharp
using DlcSharp;

var decoder = new DlcDecoder();

// The library does not read files itself: open the file and pass the stream.
await using var stream = File.OpenRead("links.dlc");
DlcContainer container = await decoder.DecodeAsync(stream);

foreach (DlcPackage package in container.Packages)
{
    Console.WriteLine($"{package.Name} ({package.Files.Count} files)");

    foreach (DlcFile file in package.Files)
    {
        Console.WriteLine($"  {file.FileName}  {file.Size} bytes  {file.Url}");
    }
}

string json = container.ToJson();   // camelCase, nulls omitted, ContainerKey not included
```

`DecodeAsync` has three overloads: `string` (the text of the file), `byte[]` and `Stream` (left open). Each takes an optional `CancellationToken`.
Whitespace, line breaks and a byte order mark in the input are ignored. One decoder can be reused for many files and from several threads.

### The data model

| Type           | Members                                                                            |
|----------------|------------------------------------------------------------------------------------|
| `DlcContainer` | `Header`, `Packages`, `ContainerKey`, `ToJson(bool indented = true)`               |
| `DlcHeader`    | `GeneratorApp`, `GeneratorVersion`, `GeneratorUrl`, `TributeName`, `DlcXmlVersion` |
| `DlcPackage`   | `Name`, `Passwords`, `Comment`, `Category`, `Files`                                |
| `DlcFile`      | `Url`, `FileName`, `Size`                                                          |

Missing or empty values are `null`, including a file's `Url`. `Passwords` is the exception: it is a `string[]` and never `null`. A package without passwords gives an empty array (a real container from filecrypt.cc stores `{}` for that), and a list such as `{"a","b"}` or `["a","b"]` is split into its items. A plain text without brackets counts as one single password.

## Open the same file again offline

A container is decrypted with a key that only the key service can hand out (see [How it works](#how-it-works)).
`DlcContainer.ContainerKey` is that key. Keep it next to the file, and you can decode the same container later without any network access:

```csharp
string content = File.ReadAllText("links.dlc");

DlcContainer first = await decoder.DecodeAsync(content);
byte[] key = first.ContainerKey;                       // 16 bytes, keep it

DlcContainer again = DlcDecoder.Decode(content, key);  // static: needs no decoder, makes no request
```

## Options

```csharp
var options = new DlcDecoderOptions
{
    ServiceUrl = new Uri("https://my-proxy.example/dlcrypt/service.php"),  // default: the JDownloader service over HTTPS
    AppName = "pylo",                                       // sent as destType
    AppKey = Encoding.ASCII.GetBytes("cb99b5cbc24db398"),   // 16 characters, used as ASCII bytes (not hex)
    AppIv = Encoding.ASCII.GetBytes("9bc24cb995cb8db3"),    // 16 characters, used as ASCII bytes (not hex)
    Timeout = TimeSpan.FromSeconds(10),                     // default 30 seconds
};

// Optionally bring your own HttpClient (proxy, logging, IHttpClientFactory). It is not disposed.
var decoder = new DlcDecoder(new HttpClient(), options);
```

Without an `HttpClient` a shared static client is used, so creating many decoders does not leak sockets.
The options are validated and copied when the decoder is created. `AppName`, `AppKey` and `AppIv` belong together.

## Errors

All exceptions derive from `DlcException`. The class is `closed`, so a `switch` over `DlcFormatException` and `DlcServiceException` needs no default arm.

| Exception                    | When                                                                                                                                                                                                                          |
|------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DlcFormatException`         | The input is too short (88 characters or fewer) or not Base64, the payload length is not a multiple of 16, the decrypted data is not XML (usually a wrong key), or the XML is malformed, has a DTD, or has no content element |
| `DlcServiceException`        | The key service failed. Check `Error`: `Unreachable`, `Timeout`, `HttpError` (see `StatusCode`), `MissingKey` (answer without a key, for example a rate limit or a blocked app name) or `InvalidKey`                          |
| `OperationCanceledException` | Your `CancellationToken` was cancelled                                                                                                                                                                                        |

Invalid input is rejected before any request is sent.

## How it works

1. The file is Base64 text. Its last 88 characters are the *key part*, the rest is the encrypted payload.
2. The key part is sent with an HTTPS GET to the key service (`srcType=dlc&destType=<AppName>&data=<key part, URL-encoded>`). The answer contains `<rc>Base64</rc>`, and its first 16 bytes are the wrapped container key.
3. The wrapped key is decrypted with AES-128-CBC (no padding) using the app key and IV. This yields the container key.
4. The payload is decrypted with AES-128-CBC (no padding), with the container key as both key and IV. The result is Base64 text that decodes to XML.
5. In that XML, the package attributes (`name`, `passwords`, `comment`, `category`) and the file elements (`url`, `filename`, `size`) are Base64 encoded once more. The library decodes them.

The service step cannot be skipped for new files: there is no offline way to get the container key.

## Examples

Short, runnable examples live in [examples/DlcSharp.Examples](https://github.com/MCmoderSD/DLC-Sharp/tree/master/examples/DlcSharp.Examples). Run one from a clone of the repository, with a `.dlc` file of your own:

```bash
dotnet run --project examples/DlcSharp.Examples -- basic links.dlc
```

| Example   | What it shows                                                                                                                       |
|-----------|-------------------------------------------------------------------------------------------------------------------------------------|
| `basic`   | Open a file, list packages, files, sizes and links, and turn the result into JSON. Also shows the text and bytes forms of the input |
| `offline` | Save the container key once, then open the same file without network                                                                |
| `options` | Change the timeout, address or application credentials, and bring your own `HttpClient` that prints what is sent                    |
| `errors`  | Handle a broken file, a failing key service and cancelling                                                                          |

## Known limits

- **Network and privacy:** Decoding needs one request to a third-party service. The key part of your file (not the payload) is sent in it, over HTTPS by default. The `options` example prints exactly what is sent. `ServiceUrl` can be pointed at your own proxy.
- **Availability:** The service may rate-limit per IP address, and application names or keys can be blocked or rotated at any time. That is why `AppName`, `AppKey`, `AppIv` and `ServiceUrl` are configurable.
- **Built-in constants:** The default app name (`pylo`), key and IV are the values of the open-source download manager [pyLoad](https://github.com/pyload/pyload), taken from its DLC plugin (`DLC.py`, GPLv3). Only these protocol values are used, no pyLoad code. They are not an official release from JDownloader or AppWork, and the key service sees every request as coming from pyLoad. We found no public way to request credentials of your own. If you have some, set `AppName`, `AppKey` and `AppIv`. Use the defaults only where that is acceptable for you.
- **JSON and HTML:** `ToJson` keeps non-ASCII characters readable and does not escape `<`, `>`, `&` or `'`. That is valid JSON, but do not place the output unchanged into an HTML page or a `<script>` block: names, comments and passwords come from the file and can contain anything.
- **Decrypt only:** Creating DLC containers is not supported.

## Tests

```bash
dotnet test
```

The tests use xUnit v3 on .NET 11. They need no network and no real `.dlc` file: they build encrypted containers themselves and play the key service with a fake `HttpMessageHandler`.

Line and branch coverage of `DlcSharp.dll` is 100 %. Source-generated code (the regular expression and the JSON serializers) is not counted. To measure it:

```bash
dotnet test -- --coverage --coverage-output-format cobertura --coverage-settings tests/DlcSharp.Tests/coverage.config
```

The report is written to the `TestResults` folder.

## License

BSD 3-Clause, see [LICENSE](https://github.com/MCmoderSD/DLC-Sharp/blob/master/LICENSE). "JDownloader" and "DLC" are names of their respective owners and are used here only to describe compatibility.
