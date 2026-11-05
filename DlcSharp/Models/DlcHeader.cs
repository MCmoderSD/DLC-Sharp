namespace DlcSharp
{
    public sealed class DlcHeader
    {
        public string? GeneratorApp { get; init; }

        public string? GeneratorVersion { get; init; }

        public string? GeneratorUrl { get; init; }

        public string? TributeName { get; init; }

        public string? DlcXmlVersion { get; init; }
    }
}