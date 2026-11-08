using System.Security.Cryptography;
using System.Text;

namespace DlcSharp.Tests
{
    internal static class DlcTestFactory
    {
        private static readonly byte[] DefaultAppKey = [.. "cb99b5cbc24db398"u8];
        private static readonly byte[] DefaultAppIv = [.. "9bc24cb995cb8db3"u8];

        public static byte[] ContainerKey(byte seed = 1)
        {
            var key = new byte[16];
            for (var i = 0; i < key.Length; i++)
            {
                key[i] = (byte) (seed * 31 + i * 17 + 3);
            }

            return key;
        }

        public static string Encode(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public static string KeyPart()
        {
            var bytes = new byte[64];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte) (i * 7 + 1);
            }

            byte[] special = [0xFB, 0xEF, 0xBE, 0xFF, 0xFF, 0xFF];
            special.CopyTo(bytes, 0);
            return Convert.ToBase64String(bytes);
        }

        public static string FileXml(string? url, string? fileName = null, long? size = null)
        {
            var xml = "<file>";
            if (url != null)
            {
                xml += $"<url>{Encode(url)}</url>";
            }

            if (fileName != null)
            {
                xml += $"<filename>{Encode(fileName)}</filename>";
            }

            if (size != null)
            {
                xml += $"<size>{Encode(size.Value.ToString())}</size>";
            }

            return xml + "</file>";
        }

        public static string PackageXml(string? name, string? passwords, string? comment, string? category, params string[] files)
        {
            var xml = "<package";
            if (name != null)
            {
                xml += $" name=\"{Encode(name)}\"";
            }

            if (passwords != null)
            {
                xml += $" passwords=\"{Encode(passwords)}\"";
            }

            if (comment != null)
            {
                xml += $" comment=\"{Encode(comment)}\"";
            }

            if (category != null)
            {
                xml += $" category=\"{Encode(category)}\"";
            }

            return xml + ">" + string.Concat(files) + "</package>";
        }

        private static string HeaderXml()
        {
            return "<header>"
                   + $"<generator><app>{Encode("TestGenerator")}</app><version>{Encode("1.2.3")}</version><url>{Encode("https://generator.test")}</url></generator>"
                   + $"<tribute><name>{Encode("tester")}</name></tribute>"
                   + $"<dlcxmlversion>{Encode("20_02_2008")}</dlcxmlversion>"
                   + "</header>";
        }

        public static string ContainerXml(string packages)
        {
            return $"<dlc>{HeaderXml()}<content>{packages}</content></dlc>";
        }

        public static string SampleXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                   + ContainerXml(
                       PackageXml(
                           "Müller Paket",
                           "geheim",
                           "Ein Kommentar",
                           "movies",
                           FileXml("https://host.test/a.part1.rar", "a.part1.rar", 1234567890123),
                           FileXml("https://host.test/ä ö ü.txt", "ä ö ü.txt", 42))
                       + PackageXml("Zweites Paket", null, null, null, FileXml("https://host.test/b.bin", "b.bin")));
        }

        private static byte[] EncryptPayload(string xml, byte[] containerKey)
        {
            var inner = Encoding.ASCII.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes(xml)));
            var padded = new byte[(inner.Length + 15) / 16 * 16];
            
            inner.CopyTo(padded, 0);
            
            return AesEncrypt(padded, containerKey, containerKey);
        }

        public static string BuildDlc(string xml, byte[] containerKey)
        {
            return Convert.ToBase64String(EncryptPayload(xml, containerKey)) + KeyPart();
        }

        public static string ServiceBody(byte[] containerKey, byte[]? appKey = null, byte[]? appIv = null)
        {
            var wrapped = AesEncrypt(containerKey, appKey ?? DefaultAppKey, appIv ?? DefaultAppIv);
            var withExtra = wrapped.Concat(Enumerable.Repeat((byte) 0x55, 16)).ToArray();
            
            return "<?xml version=\"1.0\"?><rc>" + Convert.ToBase64String(withExtra) + "</rc>";
        }

        public static byte[] AesEncrypt(byte[] data, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            
            aes.Key = key;
            
            return aes.EncryptCbc(data, iv, PaddingMode.None);
        }
    }
}