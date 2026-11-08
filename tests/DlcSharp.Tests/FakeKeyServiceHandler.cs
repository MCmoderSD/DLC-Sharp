using System.Net;

namespace DlcSharp.Tests
{
    internal sealed class FakeKeyServiceHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        public static FakeKeyServiceHandler ReturningKey(byte[] containerKey, byte[]? appKey = null, byte[]? appIv = null)
        {
            return ReturningBody(DlcTestFactory.ServiceBody(containerKey, appKey, appIv));
        }

        public static FakeKeyServiceHandler ReturningBody(string body)
        {
            return new FakeKeyServiceHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }));
        }

        public static FakeKeyServiceHandler ReturningStatus(HttpStatusCode status)
        {
            return new FakeKeyServiceHandler((_, _) => Task.FromResult(new HttpResponseMessage(status)));
        }

        public static FakeKeyServiceHandler Throwing(Exception exception)
        {
            return new FakeKeyServiceHandler((_, _) => throw exception);
        }

        public static FakeKeyServiceHandler Hanging()
        {
            return new FakeKeyServiceHandler(async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        }

        public static FakeKeyServiceHandler MustNotBeCalled()
        {
            return Throwing(new InvalidOperationException("The key service must not be contacted."));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return respond(request, cancellationToken);
        }
    }
}