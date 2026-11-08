using DlcSharp.Internal;

namespace DlcSharp.Tests
{
    public class DlcInputTests
    {
        private static string KeyPart => DlcTestFactory.KeyPart();

        [Fact]
        public void Parse_SplitsOffTheLastEightyEightCharactersAsKeyPart()
        {
            var input = DlcInput.Parse("A" + KeyPart);

            Assert.Equal("A", input.Payload);
            Assert.Equal(KeyPart, input.KeyPart);
        }

        [Fact]
        public void Parse_RemovesWhitespaceAndByteOrderMarks()
        {
            var input = DlcInput.Parse("\uFEFF PAY\r\nLOAD0\t" + KeyPart.Insert(10, "\n").Insert(50, "  ") + " \uFEFF\n");

            Assert.Equal("PAYLOAD0", input.Payload);
            Assert.Equal(KeyPart, input.KeyPart);
        }

        [Fact]
        public void Parse_KeepsPaddingCharacters()
        {
            var input = DlcInput.Parse("AB==" + KeyPart);

            Assert.Equal("AB==", input.Payload);
            Assert.EndsWith("==", input.KeyPart);
        }

        [Fact]
        public void Parse_RejectsInputThatIsNotLongerThanTheKeyPart()
        {
            var exception = Assert.Throws<DlcFormatException>(() => DlcInput.Parse(KeyPart));

            Assert.Contains("too short", exception.Message);
            Assert.Contains("only 88 were found", exception.Message);
        }

        [Theory]
        [InlineData('-')]
        [InlineData('_')]
        [InlineData('<')]
        public void Parse_RejectsCharactersThatAreNotBase64(char invalid)
        {
            var exception = Assert.Throws<DlcFormatException>(() => DlcInput.Parse("PAYLOAD" + invalid + "0" + KeyPart));

            Assert.Contains($"'{invalid}'", exception.Message);
        }
    }
}