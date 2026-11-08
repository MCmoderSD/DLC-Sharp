using DlcSharp.Internal;

namespace DlcSharp.Tests
{
    public class DlcCryptoTests
    {
        [Fact]
        public void DecryptCbcNoPadding_MatchesTheOfficialAesTestVector()
        {
            var key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
            var cipher = Convert.FromHexString("69c4e0d86a7b0430d8cdb78070b4c55a");
            var plain = DlcCrypto.DecryptCbcNoPadding(cipher, key, new byte[16]);

            Assert.Equal(Convert.FromHexString("00112233445566778899aabbccddeeff"), plain);
        }

        [Fact]
        public void DecryptCbcNoPadding_ReversesCbcEncryptionOfSeveralBlocks()
        {
            var key = DlcTestFactory.ContainerKey(3);
            var iv = DlcTestFactory.ContainerKey(4);
            var plain = Enumerable.Range(0, 64).Select(i => (byte) i).ToArray();
            var result = DlcCrypto.DecryptCbcNoPadding(DlcTestFactory.AesEncrypt(plain, key, iv), key, iv);

            Assert.Equal(plain, result);
        }
    }
}