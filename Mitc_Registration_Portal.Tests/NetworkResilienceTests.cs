using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Implementation.Services;
using Xunit;

namespace Mitc_Registration_Portal.Tests
{
    public class NetworkResilienceTests
    {
        private readonly IOptions<PaystackConfiguration> _options;

        public NetworkResilienceTests()
        {
            _options = Options.Create(new PaystackConfiguration
            {
                SecretKey = "sk_test_mock_secret_key_12345",
                CallbackUrl = "https://localhost:7082/Payment/Callback",
                RegistrationFeeNaira = 5000.00m
            });
        }

        [Fact]
        public async Task Initialize_WhenNetworkIsOffline_ReturnsCleanFailureWithoutThrowing()
        {
            // Arrange
            var mockHandler = MockHttpMessageHandler.SimulatingNetworkOffline();
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.paystack.co/") };
            var service = new PayStackService(httpClient, _options, NullLogger<PayStackService>.Instance);

            // Act
            var result = await service.Initialize("REG-TEST-REF-01", 5000.00m, "student@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Status);
            Assert.Contains("internet connection", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Verify_WhenNetworkIsOffline_ReturnsCleanFailureWithoutThrowing()
        {
            // Arrange
            var mockHandler = MockHttpMessageHandler.SimulatingNetworkOffline();
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.paystack.co/") };
            var service = new PayStackService(httpClient, _options, NullLogger<PayStackService>.Instance);

            // Act
            var result = await service.Verify("REG-TEST-REF-01");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Status);
            Assert.Contains("internet connection", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Initialize_WhenGatewayTimesOut_ReturnsCleanTimeoutFailure()
        {
            // Arrange
            var mockHandler = MockHttpMessageHandler.SimulatingTimeout();
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.paystack.co/") };
            var service = new PayStackService(httpClient, _options, NullLogger<PayStackService>.Instance);

            // Act
            var result = await service.Initialize("REG-TEST-REF-02", 5000.00m, "student@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Status);
            Assert.Contains("timed out", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Verify_WhenGatewayTimesOut_ReturnsCleanTimeoutFailure()
        {
            // Arrange
            var mockHandler = MockHttpMessageHandler.SimulatingTimeout();
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.paystack.co/") };
            var service = new PayStackService(httpClient, _options, NullLogger<PayStackService>.Instance);

            // Act
            var result = await service.Verify("REG-TEST-REF-02");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Status);
            Assert.Contains("timed out", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Initialize_WhenGatewayReturns503_ReturnsFriendlyServiceUnavailableMessage()
        {
            // Arrange
            var mockHandler = MockHttpMessageHandler.SimulatingServerError(HttpStatusCode.ServiceUnavailable);
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.paystack.co/") };
            var service = new PayStackService(httpClient, _options, NullLogger<PayStackService>.Instance);

            // Act
            var result = await service.Initialize("REG-TEST-REF-03", 5000.00m, "student@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Status);
            Assert.Contains("unreachable", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Initialize_WhenGatewayReturnsMaliciousOrLoopbackUrl_RejectsRedirect()
        {
            // Arrange
            var mockHandler = MockHttpMessageHandler.SimulatingSuccessfulInitialize(
                "REG-TEST-REF-04",
                "http://localhost:5000/evil-redirect");
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.paystack.co/") };
            var service = new PayStackService(httpClient, _options, NullLogger<PayStackService>.Instance);

            // Act
            var result = await service.Initialize("REG-TEST-REF-04", 5000.00m, "student@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Status);
            Assert.Contains("not return a valid payment page", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
