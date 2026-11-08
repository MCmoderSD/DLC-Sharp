using System.Text;

namespace DlcSharp.Internal;

internal readonly struct DlcInput
{
    private const int KeyPartLength = 88;

    public string Payload { get; }
    public string KeyPart { get; }
    
    private DlcInput(string payload, string keyPart)
    {
        Payload = payload;
        KeyPart = keyPart;
    }

    public static DlcInput Parse(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text.Where(c => !char.IsWhiteSpace(c) && c != '\uFEFF'))
        {
            if (!LenientBase64.IsAlphabet(c) && c != '=')
            {
                throw new DlcFormatException($"The input is not a DLC container: it contains characters that are not valid Base64 (for example '{c}').");
            }

            sb.Append(c);
        }

        if (sb.Length <= KeyPartLength)
        {
            throw new DlcFormatException($"The input is too short to be a DLC container: more than {KeyPartLength} characters are required, but only {sb.Length} were found.");
        }

        var normalized = sb.ToString();
        var split = normalized.Length - KeyPartLength;
        return new DlcInput(normalized[..split], normalized[split..]);
    }

    public static string ReadText(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    public static async Task<string> ReadTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }
}