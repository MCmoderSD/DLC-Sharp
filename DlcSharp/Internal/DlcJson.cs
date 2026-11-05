using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DlcSharp.Internal
{
    [JsonSerializable(typeof(DlcContainer))]
    internal sealed partial class DlcJsonContext : JsonSerializerContext;

    internal static class DlcJson
    {
        public static DlcJsonContext Compact { get; } = Create(indented: false);

        public static DlcJsonContext Indented { get; } = Create(indented: true);

        private static DlcJsonContext Create(bool indented)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = indented,
            };
            return new DlcJsonContext(options);
        }
    }
}