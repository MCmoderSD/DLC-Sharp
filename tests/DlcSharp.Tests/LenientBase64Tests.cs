using System.Text;
using DlcSharp.Internal;

namespace DlcSharp.Tests;

public class LenientBase64Tests
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    private static string DecodeToAscii(string input)
    {
        return Encoding.ASCII.GetString(LenientBase64.Decode(input));
    }

    [Fact]
    public void IsAlphabetIsTrueForExactlyTheSixtyFourBase64Characters()
    {
        var accepted = new List<char>();
        for (var code = 0; code <= char.MaxValue; code++)
        {
            if (LenientBase64.IsAlphabet((char) code))
            {
                accepted.Add((char) code);
            }
        }

        Assert.Equal(Alphabet.Order().ToArray(), accepted.Order().ToArray());
    }

    [Theory]
    [InlineData("QQ==", "A")]
    [InlineData("QUI=", "AB")]
    [InlineData("QUJD", "ABC")]
    public void DecodeDecodesPaddedBase64(string input, string expected)
    {
        Assert.Equal(expected, DecodeToAscii(input));
    }

    [Theory]
    [InlineData("QQ", "A")]
    [InlineData("QUI", "AB")]
    public void DecodeAcceptsMissingPadding(string input, string expected)
    {
        Assert.Equal(expected, DecodeToAscii(input));
    }

    [Theory]
    [InlineData("QUJD\r\nREVG", "ABCDEF")]  // line breaks of a DLC file
    [InlineData("\0QUJD\0", "ABC")]         // NUL padding of the decrypted payload
    [InlineData("QU-JD_", "ABC")]           // characters of the URL-safe alphabet
    public void DecodeSkipsCharactersThatAreNotPartOfTheAlphabet(string input, string expected)
    {
        Assert.Equal(expected, DecodeToAscii(input));
    }

    [Theory]
    [InlineData("QUI=QUJD", "AB")]
    [InlineData("==QUJD", "")]
    public void DecodeStopsAtTheFirstPaddingCharacter(string input, string expected)
    {
        Assert.Equal(expected, DecodeToAscii(input));
    }

    [Theory]
    [InlineData("QUJDR", "ABC")]
    [InlineData("A", "")]
    public void DecodeDropsASingleLeftoverCharacter(string input, string expected)
    {
        Assert.Equal(expected, DecodeToAscii(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!")]
    public void DecodeReturnsNothingWhenNoAlphabetCharactersRemain(string input)
    {
        Assert.Empty(LenientBase64.Decode(input));
    }

    [Fact]
    public void DecodeHandlesPlusAndSlash()
    {
        Assert.Equal(new byte[] { 0xFB, 0xEF, 0xBE, 0xFF, 0xFF, 0xFF }, LenientBase64.Decode("++++////"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DecodeFieldOrRawReturnsAnEmptyStringForEmptyInput(string raw)
    {
        Assert.Equal(string.Empty, LenientBase64.DecodeFieldOrRaw(raw));
    }

    [Theory]
    [InlineData("TWE=", "Ma")]
    [InlineData("TWFu", "Man")]
    [InlineData("  TWFu\r\n", "Man")]
    public void DecodeFieldOrRawDecodesBase64Text(string raw, string expected)
    {
        Assert.Equal(expected, LenientBase64.DecodeFieldOrRaw(raw));
    }

    [Fact]
    public void DecodeFieldOrRawDecodesTextThatContainsNonAsciiCharacters()
    {
        var raw = DlcTestFactory.Encode("Müller – ä ö ü ß 日本語");

        Assert.Equal("Müller – ä ö ü ß 日本語", LenientBase64.DecodeFieldOrRaw(raw));
    }

    [Theory]
    [InlineData("not base64")]  // length is no multiple of four
    [InlineData(" 42 ")]        // the same, and the input comes back untrimmed
    [InlineData("A===")]        // more than two padding characters
    [InlineData("====")]        // nothing but padding
    [InlineData("ab!=")]        // a character outside the alphabet
    [InlineData("QQ==QQ==")]    // padding in the middle
    [InlineData("//7/")]        // valid Base64, but the bytes are no valid UTF-8
    [InlineData("1234")]        // a plain number that happens to be valid Base64
    public void DecodeFieldOrRawReturnsTheInputWhenItIsNoBase64EncodedText(string raw)
    {
        Assert.Equal(raw, LenientBase64.DecodeFieldOrRaw(raw));
    }
}