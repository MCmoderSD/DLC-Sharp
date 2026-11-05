using System.Text.Json;
using System.Text.Json.Serialization;
using DlcSharp.Internal;

namespace DlcSharp
{
    public sealed class DlcContainer
    {
        public DlcHeader Header { get; init; } = new();

        public IReadOnlyList<DlcPackage> Packages { get; init; } = [];

        [JsonIgnore]
        public byte[] ContainerKey { get; init; } = [];

        public string ToJson(bool indented = true)
        {
            return indented
                ? JsonSerializer.Serialize(this, DlcJson.Indented.DlcContainer)
                : JsonSerializer.Serialize(this, DlcJson.Compact.DlcContainer);
        }
    }
}