using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Implementation.Services;
using Xunit;

namespace Mitc_Registration_Portal.Tests
{
    public class FinancialPrecisionTests
    {
        private readonly PaystackConfiguration _config;
        private readonly IOptions<PaystackConfiguration> _options;

        public FinancialPrecisionTests()
        {
            _config = new PaystackConfiguration
            {
                SecretKey = "sk_test_mock_secret_key_12345",
                CallbackUrl = "https://localhost:7082/Payment/Callback",
                RegistrationFeeNaira = 5000.00m
            };
            _options = Options.Create(_config);
        }

        [Fact]
        public void RegistrationFeeKobo_CalculatesExactIntegerKoboWithoutFloatingPointError()
        {
            // 5000.00 Naira = 500,000 kobo
            Assert.Equal(500000, _config.RegistrationFeeKobo);

            var customFee = new PaystackConfiguration { RegistrationFeeNaira = 1234.56m };
            Assert.Equal(123456, customFee.RegistrationFeeKobo);
        }

        [Theory]
        [InlineData(5000.00, true)]
        [InlineData(5000.01, false)]
        [InlineData(4999.99, false)]
        [InlineData(0.0, false)]
        public void MatchesExpectedFee_ValidatesExactNaira(decimal amount, bool expected)
        {
            Assert.Equal(expected, _config.MatchesExpectedFee(amount));
        }

        [Theory]
        [InlineData(500000, true)]
        [InlineData(500001, false)]
        [InlineData(499999, false)]
        [InlineData(100, false)]
        public void MatchesExpectedFeeKobo_ValidatesExactKobo(long kobo, bool expected)
        {
            Assert.Equal(expected, _config.MatchesExpectedFeeKobo(kobo));
        }

        [Fact]
        public async Task Verify_WhenAmountIsUnderpaid_RejectsVerificationEvenIfStatusIsSuccess()
        {
            // Simulates a user who somehow triggered success for 100 kobo (1 Naira) instead of 5,000 Naira
            var mockHandler = MockHttpMessageHandler.SimulatingSuccessfulVerify("REG-UNDERPAID-01", amountKobo: 100);
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.paystack.co/") };
            var service = new PayStackService(httpClient, _options, NullLogger<PayStackService>.Instance);

            var result = await service.Verify("REG-UNDERPAID-01");

            Assert.NotNull(result);
            Assert.False(result.Status);
            Assert.Contains("did not match", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Verify_WhenAmountMatchesExactFee_AcceptsVerification()
        {
            var mockHandler = MockHttpMessageHandler.SimulatingSuccessfulVerify("REG-VALID-01", amountKobo: 500000);
            var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("https://api.paystack.co/") };
            var service = new PayStackService(httpClient, _options, NullLogger<PayStackService>.Instance);

            var result = await service.Verify("REG-VALID-01");

            Assert.NotNull(result);
            Assert.True(result.Status);
            Assert.Equal("Payment Verified", result.Message);
            Assert.Equal(500000, result.Data.AmountKobo);
        }
    }
}
