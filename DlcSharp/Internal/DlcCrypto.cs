using System.Security.Cryptography;

namespace DlcSharp.Internal
{
    internal static class DlcCrypto
    {
        public const int BlockSize = 16;

        public static byte[] DecryptCbcNoPadding(byte[] data, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.KeySize = 128;
            aes.Key = key;
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }
}