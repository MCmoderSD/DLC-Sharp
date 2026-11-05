using System.Text;

namespace DlcSharp.Internal
{
    internal static class LenientBase64
    {
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        public static bool IsAlphabet(char c)
        {
            return c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '/';
        }

        public static byte[] Decode(string text)
        {
            var sb = new StringBuilder(text.Length + 4);
            foreach (var c in text.TakeWhile(c => c != '=').Where(IsAlphabet))
            {
                sb.Append(c);
            }

            var remainder = sb.Length % 4;
            if (remainder == 1)
            {
                sb.Length--;
            }
            else if (remainder != 0)
            {
                sb.Append('=', 4 - remainder);
            }

            return sb.Length == 0 ? [] : Convert.FromBase64String(sb.ToString());
        }

        public static string DecodeFieldOrRaw(string raw)
        {
            var s = raw.Trim();
            if (s.Length == 0)
            {
                return string.Empty;
            }

            if (s.Length % 4 != 0)
            {
                return raw;
            }

            var end = s.Length;
            while (end > 0 && s[end - 1] == '=')
            {
                end--;
            }

            if (s.Length - end > 2)
            {
                return raw;
            }

            for (var i = 0; i < end; i++)
            {
                if (!IsAlphabet(s[i]))
                {
                    return raw;
                }
            }

            try
            {
                return StrictUtf8.GetString(Convert.FromBase64String(s));
            }
            catch (FormatException)
            {
                return raw;
            }
            catch (DecoderFallbackException)
            {
                return raw;
            }
        }
    }
}