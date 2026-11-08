using System.Text.Json;
using System.Text.Json.Serialization;
using DlcSharp.Internal;

namespace DlcSharp;

/// <summary>
/// A decrypted DLC container: information about its creator and the packages with their files.
/// </summary>
public sealed class DlcContainer
{
    /// <summary>
    /// Information about the program that created the container.
    /// </summary>
    public DlcHeader Header { get; init; } = new();

    /// <summary>
    /// The packages, in the order of the file.
    /// </summary>
    public IReadOnlyList<DlcPackage> Packages { get; init; } = [];

    /// <summary>
    /// The 16-byte key that decrypts this container. Keep it to open the same file again without network access,
    /// see <see cref="DlcDecoder.Decode"/>. It is not part of the JSON output.
    /// </summary>
    [JsonIgnore]
    public byte[] ContainerKey { get; init; } = [];

    /// <summary>
    /// Writes the container as JSON.
    /// </summary>
    /// <remarks>
    /// Names are camelCase, values that are <see langword="null"/> are left out and the container key is not included.
    /// Non-ASCII characters stay readable, and <c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c> and <c>'</c> are not escaped,
    /// so do not place the result unchanged into an HTML page.
    /// </remarks>
    /// <param name="indented"><see langword="true"/> for readable output with indentation, <see langword="false"/> for a single line.</param>
    /// <returns>The JSON text.</returns>
    public string ToJson(bool indented = true)
    {
        return indented
            ? JsonSerializer.Serialize(this, DlcJson.Indented.DlcContainer)
            : JsonSerializer.Serialize(this, DlcJson.Compact.DlcContainer);
    }
}