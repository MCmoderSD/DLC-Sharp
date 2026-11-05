# DlcSharp

A small .NET library that opens **JDownloader DLC containers**. You give it a `.dlc` file and get back the packages, file names, sizes and download links as plain objects, or as JSON.

- Targets `net10.0` and has no third-party dependencies: AES comes from `System.Security.Cryptography`, JSON from `System.Text.Json`.
- Plain model classes. The JSON output uses serializers generated at compile time, so it needs no reflection.
- Written from scratch against the public format, without code from other DLC libraries.

> **Not affiliated with JDownloader or AppWork.** See [Known limits](#known-limits).

## Installation

DlcSharp is not on NuGet yet. Until then, clone this repository and reference the project:

```bash
dotnet add reference path/to/DlcSharp/DlcSharp/DlcSharp.csproj
```

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

| Type | Members |
| --- | --- |
| `DlcContainer` | `Header`, `Packages`, `ContainerKey`, `ToJson(bool indented = true)` |
| `DlcHeader` | `GeneratorApp`, `GeneratorVersion`, `GeneratorUrl`, `TributeName`, `DlcXmlVersion` |
| `DlcPackage` | `Name`, `Passwords`, `Comment`, `Category`, `Files` |
| `DlcFile` | `Url`, `FileName`, `Size` |

Missing or empty values are `null`, including a file's `Url`. `Passwords` is the exception: it is a `string[]` and never `null`. A package without passwords gives an empty array (a real container from filecrypt.cc stores `{}` for that), and a list such as `{"a","b"}` or `["a","b"]` is split into its items. A plain text without brackets counts as one single password.

## Open the same file again offline

A container is decrypted with a key that only the key service can hand out (see [How it works](#how-it-works)).
`DlcContainer.ContainerKey` is that key. Keep it next to the file and you can decode the same container later without any network access:

```csharp
string content = File.ReadAllText("links.dlc");

DlcContainer first = await decoder.DecodeAsync(content);
byte[] key = first.ContainerKey;                    // 16 bytes, keep it

DlcContainer again = decoder.Decode(content, key);  // no request is made
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

All exceptions derive from `DlcException`.

| Exception | When |
| --- | --- |
| `DlcFormatException` | The input is too short (88 characters or fewer) or not Base64, the payload length is not a multiple of 16, the decrypted data is not XML (usually a wrong key), or the XML is malformed, has a DTD, or has no content element |
| `DlcServiceException` | The key service failed. Check `Error`: `Unreachable`, `Timeout`, `HttpError` (see `StatusCode`), `MissingKey` (answer without a key, for example a rate limit or a blocked app name) or `InvalidKey` |
| `OperationCanceledException` | Your `CancellationToken` was cancelled |

Invalid input is rejected before any request is sent.

## How it works

1. The file is Base64 text. Its last 88 characters are the *key part*, the rest is the encrypted payload.
2. The key part is sent with an HTTPS GET to the key service (`srcType=dlc&destType=<AppName>&data=<key part, URL-encoded>`). The answer contains `<rc>Base64</rc>`, and its first 16 bytes are the wrapped container key.
3. The wrapped key is decrypted with AES-128-CBC (no padding) using the app key and IV. This yields the container key.
4. The payload is decrypted with AES-128-CBC (no padding), with the container key as both key and IV. The result is Base64 text that decodes to XML.
5. In that XML, the package attributes (`name`, `passwords`, `comment`, `category`) and the file elements (`url`, `filename`, `size`) are Base64 encoded once more. The library decodes them.

The service step cannot be skipped for new files: there is no offline way to get the container key.

## Examples

Short, runnable examples live in [examples/DlcSharp.Examples](examples/DlcSharp.Examples). Run one with a `.dlc` file of your own:

```bash
dotnet run --project examples/DlcSharp.Examples -- basic links.dlc
```

| Example | What it shows |
| --- | --- |
| `basic` | Open a file, list packages, files, sizes and links, and turn the result into JSON. Also shows the text and bytes forms of the input |
| `offline` | Save the container key once, then open the same file without network |
| `options` | Change the timeout, address or application credentials, and bring your own `HttpClient` that prints what is sent |
| `errors` | Handle a broken file, a failing key service and cancelling |

## Known limits

- **Network and privacy:** Decoding needs one request to a third-party service. The key part of your file (not the payload) is sent in it, over HTTPS by default. The `options` example prints exactly what is sent. `ServiceUrl` can be pointed at your own proxy.
- **Availability:** The service may rate-limit per IP address, and application names or keys can be blocked or rotated at any time. That is why `AppName`, `AppKey`, `AppIv` and `ServiceUrl` are configurable.
- **Built-in constants:** The default app name (`pylo`), key and IV are taken from the DLC plugin of the open-source download manager [pyLoad](https://github.com/pyload/pyload) (`DLC.py`, GPLv3), where `pylo` is pyLoad's own application name on the JDownloader service. Only these protocol values are used, no pyLoad code. There is no official release of them from JDownloader or AppWork, so the service sees requests under pyLoad's application name. Use the library only where that is acceptable for you, or request your own application credentials from the JDownloader developers and set `AppName`, `AppKey` and `AppIv` accordingly.
- **Decrypt only:** Creating DLC containers is not supported.

## License

BSD 3-Clause, see [LICENSE](LICENSE). "JDownloader" and "DLC" are names of their respective owners and are used here only to describe compatibility.
