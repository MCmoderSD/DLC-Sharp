using System.Net;
using System.Text;
using DlcSharp.Internal;

namespace DlcSharp.Tests
{
    public class DlcDecoderTests
    {
        private static readonly byte[] Key = DlcTestFactory.ContainerKey();
        private static readonly string ExpectedJson = DlcXmlParser.Parse(DlcTestFactory.SampleXml(), Key).ToJson(false);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static string SampleDlc()
        {
            return DlcTestFactory.BuildDlc(DlcTestFactory.SampleXml(), Key);
        }

        private static byte[] SampleBytes(bool withByteOrderMark)
        {
            var bytes = Encoding.UTF8.GetBytes(SampleDlc());

            return withByteOrderMark ? [.. Encoding.UTF8.Preamble, .. bytes] : bytes;
        }

        private static DlcDecoder CreateDecoder(HttpMessageHandler handler, DlcDecoderOptions? options = null)
        {
            return new DlcDecoder(new HttpClient(handler), options);
        }

        private static void AssertIsSample(DlcContainer container)
        {
            Assert.Equal(ExpectedJson, container.ToJson(false));
            Assert.Equal(Key, container.ContainerKey);
        }

        [Fact]
        public async Task DecodeAsync_String_ReturnsTheDecryptedContainer()
        {
            var container = await CreateDecoder(FakeKeyServiceHandler.ReturningKey(Key)).DecodeAsync(SampleDlc(), Token);

            AssertIsSample(container);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DecodeAsync_ByteArray_ReturnsTheDecryptedContainer(bool withByteOrderMark)
        {
            var container = await CreateDecoder(FakeKeyServiceHandler.ReturningKey(Key)).DecodeAsync(SampleBytes(withByteOrderMark), Token);

            AssertIsSample(container);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DecodeAsync_Stream_ReturnsTheDecryptedContainerAndLeavesTheStreamOpen(bool withByteOrderMark)
        {
            using var stream = new MemoryStream(SampleBytes(withByteOrderMark));
            var container = await CreateDecoder(FakeKeyServiceHandler.ReturningKey(Key)).DecodeAsync(stream, Token);

            AssertIsSample(container);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task DecodeAsync_XmlWithLeadingByteOrderMarkAndWhitespace_StillDecodes()
        {
            var dlc = DlcTestFactory.BuildDlc("\uFEFF \r\n\t" + DlcTestFactory.SampleXml() + "  \n", Key);
            var container = await CreateDecoder(FakeKeyServiceHandler.ReturningKey(Key)).DecodeAsync(dlc, Token);

            AssertIsSample(container);
        }

        [Fact]
        public async Task DecodeAsync_KeyServiceAnswerWithLineBreaksInsideTheRcElement_StillWorks()
        {
            var body = DlcTestFactory.ServiceBody(Key).Replace("<rc>", "<rc>\r\n  ").Replace("</rc>", "\r\n</rc>");
            var container = await CreateDecoder(FakeKeyServiceHandler.ReturningBody(body)).DecodeAsync(SampleDlc(), Token);

            AssertIsSample(container);
        }

        [Fact]
        public async Task DecodeAsync_WithCustomAppKeyAndIv_UnwrapsTheContainerKeyWithThem()
        {
            byte[] appKey = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
            byte[] appIv = [16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1];
            
            var decoder = CreateDecoder(FakeKeyServiceHandler.ReturningKey(Key, appKey, appIv), new DlcDecoderOptions { AppKey = appKey, AppIv = appIv });
            var container = await decoder.DecodeAsync(SampleDlc(), Token);

            AssertIsSample(container);
        }

        [Theory]
        [InlineData(null, null, "https://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=pylo&data=")]
        [InlineData("http://service.test/key", null, "http://service.test/key?srcType=dlc&destType=pylo&data=")]
        [InlineData("https://service.test/key?token=abc", null, "https://service.test/key?token=abc&srcType=dlc&destType=pylo&data=")]
        [InlineData(null, "my app/ü", "https://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=my%20app%2F%C3%BC&data=")]
        public async Task DecodeAsync_AsksTheKeyServiceWithEscapedParameters(string? serviceUrl, string? appName, string expectedStart)
        {
            var options = new DlcDecoderOptions();
            if (serviceUrl != null)
            {
                options.ServiceUrl = new Uri(serviceUrl);
            }

            if (appName != null)
            {
                options.AppName = appName;
            }

            var handler = FakeKeyServiceHandler.ReturningKey(Key);
            await CreateDecoder(handler, options).DecodeAsync(SampleDlc(), Token);

            var uri = Assert.Single(handler.Requests).AbsoluteUri;
            Assert.StartsWith(expectedStart, uri);
            
            var data = uri[expectedStart.Length..];
            Assert.Equal(DlcTestFactory.KeyPart(), Uri.UnescapeDataString(data));
            Assert.DoesNotContain('+', data);
            Assert.DoesNotContain('/', data);
            Assert.DoesNotContain('=', data);
        }

        [Fact]
        public async Task DecodeAsync_InfiniteTimeout_StillDecodes()
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.ReturningKey(Key), new DlcDecoderOptions { Timeout = Timeout.InfiniteTimeSpan });
            var container = await decoder.DecodeAsync(SampleDlc(), Token);

            AssertIsSample(container);
        }

        [Fact]
        public async Task DecodeAsync_OptionsChangedAfterConstruction_DoNotAffectTheDecoder()
        {
            var handler = FakeKeyServiceHandler.ReturningKey(Key);
            var options = new DlcDecoderOptions();
            var decoder = CreateDecoder(handler, options);

            options.ServiceUrl = new Uri("http://changed.test/");
            options.AppName = "changed";
            options.AppKey[0] ^= 0xFF;
            options.AppIv[0] ^= 0xFF;
            var container = await decoder.DecodeAsync(SampleDlc(), Token);

            AssertIsSample(container);
            Assert.StartsWith("https://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=pylo&", Assert.Single(handler.Requests).AbsoluteUri);
        }

        [Fact]
        public void Decode_DecryptsWithoutContactingTheKeyService()
        {
            var handler = FakeKeyServiceHandler.MustNotBeCalled();
            var container = CreateDecoder(handler).Decode(SampleDlc(), Key);

            AssertIsSample(container);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public void Constructors_WithoutHttpClient_CreateWorkingDecoders()
        {
            AssertIsSample(new DlcDecoder().Decode(SampleDlc(), Key));
            AssertIsSample(new DlcDecoder(new DlcDecoderOptions()).Decode(SampleDlc(), Key));
        }

        [Fact]
        public void Decode_WithAContainerKeyOfTheWrongLength_ThrowsAnArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(() => new DlcDecoder().Decode(SampleDlc(), new byte[15]));

            Assert.Equal("containerKey", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsAnArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new DlcDecoder(null!));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsAnArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new DlcDecoder((HttpClient) null!));

            Assert.Equal("httpClient", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithHttpClient_AlsoValidatesTheOptions()
        {
            var exception = Assert.Throws<ArgumentException>(() => CreateDecoder(FakeKeyServiceHandler.MustNotBeCalled(), new DlcDecoderOptions { AppName = " " }));

            Assert.Equal("AppName", exception.ParamName);
        }

        [Fact]
        public async Task DecodeAsync_WithNullArguments_ThrowsAnArgumentNullException()
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.MustNotBeCalled());

            await Assert.ThrowsAsync<ArgumentNullException>(() => decoder.DecodeAsync((string) null!, Token));
            await Assert.ThrowsAsync<ArgumentNullException>(() => decoder.DecodeAsync((byte[]) null!, Token));
            await Assert.ThrowsAsync<ArgumentNullException>(() => decoder.DecodeAsync((Stream) null!, Token));
        }

        [Fact]
        public void Decode_WithNullArguments_ThrowsAnArgumentNullException()
        {
            var decoder = new DlcDecoder();

            Assert.Throws<ArgumentNullException>(() => decoder.Decode(null!, Key));
            Assert.Throws<ArgumentNullException>(() => decoder.Decode(SampleDlc(), null!));
        }

        [Fact]
        public async Task DecodeAsync_InputThatIsNoContainer_ThrowsAFormatExceptionWithoutAskingTheService()
        {
            var handler = FakeKeyServiceHandler.MustNotBeCalled();

            await Assert.ThrowsAsync<DlcFormatException>(() => CreateDecoder(handler).DecodeAsync("this is not a container!", Token));

            Assert.Empty(handler.Requests);
        }

        [Theory]
        [InlineData("AAAAAAAAAA" + "AAAAAAAAAA" + "AAA=", 17)]
        [InlineData("====", 0)]
        public async Task DecodeAsync_PayloadThatIsNotWholeBlocks_ThrowsAFormatException(string payload, int length)
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.ReturningKey(Key));
            var exception = await Assert.ThrowsAsync<DlcFormatException>(() => decoder.DecodeAsync(payload + DlcTestFactory.KeyPart(), Token));

            Assert.Contains($"({length} bytes)", exception.Message);
        }

        [Fact]
        public async Task DecodeAsync_WhenTheServiceReturnsAnotherKey_ThrowsAFormatException()
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.ReturningKey(DlcTestFactory.ContainerKey(2)));
            var exception = await Assert.ThrowsAsync<DlcFormatException>(() => decoder.DecodeAsync(SampleDlc(), Token));

            Assert.Contains("not XML", exception.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        public async Task DecodeAsync_WhenTheServiceAnswersWithAnErrorStatus_ThrowsAnHttpErrorWithTheStatusCode(HttpStatusCode status)
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.ReturningStatus(status));
            var exception = await Assert.ThrowsAsync<DlcServiceException>(() => decoder.DecodeAsync(SampleDlc(), Token));

            Assert.Equal(DlcServiceError.HttpError, exception.Error);
            Assert.Equal(status, exception.StatusCode);
            Assert.Contains($"HTTP {(int) status}", exception.Message);
        }

        [Fact]
        public async Task DecodeAsync_WhenTheServiceCannotBeReached_ThrowsAnUnreachableError()
        {
            var inner = new HttpRequestException("no route to host");
            var decoder = CreateDecoder(FakeKeyServiceHandler.Throwing(inner));
            var exception = await Assert.ThrowsAsync<DlcServiceException>(() => decoder.DecodeAsync(SampleDlc(), Token));

            Assert.Equal(DlcServiceError.Unreachable, exception.Error);
            Assert.Null(exception.StatusCode);
            Assert.Same(inner, exception.InnerException);
            Assert.Contains("no route to host", exception.Message);
        }

        [Fact]
        public async Task DecodeAsync_WhenTheServiceDoesNotAnswerInTime_ThrowsATimeoutError()
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.Hanging(), new DlcDecoderOptions { Timeout = TimeSpan.FromMilliseconds(50) });
            var exception = await Assert.ThrowsAsync<DlcServiceException>(() => decoder.DecodeAsync(SampleDlc(), Token));

            Assert.Equal(DlcServiceError.Timeout, exception.Error);
            Assert.Null(exception.StatusCode);
            Assert.IsType<OperationCanceledException>(exception.InnerException, exactMatch: false);
        }

        [Fact]
        public async Task DecodeAsync_WhenTheCallerCancels_ThrowsTheCancellationInsteadOfAServiceError()
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.Hanging());
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(Token);

            var task = decoder.DecodeAsync(SampleDlc(), cts.Token);
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task DecodeAsync_StreamWithATokenThatIsAlreadyCancelled_ThrowsBeforeReadingAnything()
        {
            var handler = FakeKeyServiceHandler.MustNotBeCalled();
            
            using var stream = new MemoryStream(SampleBytes(false));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(Token);
            
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateDecoder(handler).DecodeAsync(stream, cts.Token));

            Assert.Equal(0, stream.Position);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task DecodeAsync_WhenTheServiceAnswerHasNoRcElement_ThrowsAMissingKeyError()
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.ReturningBody("<html><body>Too many requests</body></html>"));
            var exception = await Assert.ThrowsAsync<DlcServiceException>(() => decoder.DecodeAsync(SampleDlc(), Token));

            Assert.Equal(DlcServiceError.MissingKey, exception.Error);
            Assert.Null(exception.StatusCode);
        }

        [Theory]
        [InlineData("<rc></rc>", 0)]
        [InlineData("<rc>QUJDREVGR0hJSktMTU5P</rc>", 15)]
        public async Task DecodeAsync_WhenTheServiceKeyIsTooShort_ThrowsAnInvalidKeyError(string body, int length)
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.ReturningBody(body));
            var exception = await Assert.ThrowsAsync<DlcServiceException>(() => decoder.DecodeAsync(SampleDlc(), Token));

            Assert.Equal(DlcServiceError.InvalidKey, exception.Error);
            Assert.Contains($"only {length} bytes", exception.Message);
        }

        [Fact]
        public async Task DecodeAsync_ExceptionsFromTheHandlerThatAreNotNetworkErrors_AreNotWrapped()
        {
            var decoder = CreateDecoder(FakeKeyServiceHandler.Throwing(new InvalidOperationException("unexpected")));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => decoder.DecodeAsync(SampleDlc(), Token));

            Assert.Equal("unexpected", exception.Message);
        }
    }
}