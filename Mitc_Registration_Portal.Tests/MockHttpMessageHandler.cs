using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Mitc_Registration_Portal.Tests
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }

        public static MockHttpMessageHandler SimulatingNetworkOffline()
        {
            return new MockHttpMessageHandler(_ =>
            {
                throw new HttpRequestException(
                    "No such host is known. (api.paystack.co:443)",
                    new SocketException((int)SocketError.HostNotFound));
            });
        }

        public static MockHttpMessageHandler SimulatingTimeout()
        {
            return new MockHttpMessageHandler(_ =>
            {
                throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.");
            });
        }

        public static MockHttpMessageHandler SimulatingServerError(HttpStatusCode statusCode)
        {
            return new MockHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"status\":false,\"message\":\"Internal gateway error\"}", Encoding.UTF8, "application/json")
            });
        }

        public static MockHttpMessageHandler SimulatingSuccessfulVerify(string reference, long amountKobo = 500000)
        {
            var json = $$"""
            {
                "status": true,
                "message": "Verification successful",
                "data": {
                    "id": 12345,
                    "status": "success",
                    "reference": "{{reference}}",
                    "amount": {{amountKobo}},
                    "gateway_response": "Successful",
                    "paid_at": "2026-10-18T10:00:00.000Z",
                    "channel": "card"
                }
            }
            """;

            return new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }

        public static MockHttpMessageHandler SimulatingSuccessfulInitialize(string reference, string authorizationUrl = "https://checkout.paystack.com/access-token-123")
        {
            var json = $$"""
            {
                "status": true,
                "message": "Authorization URL created",
                "data": {
                    "authorization_url": "{{authorizationUrl}}",
                    "access_code": "0peioxfhpn",
                    "reference": "{{reference}}"
                }
            }
            """;

            return new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
