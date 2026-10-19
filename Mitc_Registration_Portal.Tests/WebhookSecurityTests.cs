using System.Security.Cryptography;
using System.Text;
using Mitc_Registration_Portal.Controllers;
using Xunit;

namespace Mitc_Registration_Portal.Tests
{
    public class WebhookSecurityTests
    {
        private const string SecretKey = "sk_live_secret_sample_key_99999999999";

        private static string ComputeValidHmacHex(string body, string key)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            return Convert.ToHexStringLower(hash);
        }

        [Fact]
        public void VerifyPaystackSignature_WithAuthenticSignature_ReturnsTrue()
        {
            var body = "{\"event\":\"charge.success\",\"data\":{\"reference\":\"REG-123\",\"amount\":500000}}";
            var validSignature = ComputeValidHmacHex(body, SecretKey);

            var isValid = WebhookController.VerifyPaystackSignature(body, validSignature, SecretKey);

            Assert.True(isValid);
        }

        [Fact]
        public void VerifyPaystackSignature_WithTamperedBody_ReturnsFalse()
        {
            var originalBody = "{\"event\":\"charge.success\",\"data\":{\"reference\":\"REG-123\",\"amount\":500000}}";
            var validSignature = ComputeValidHmacHex(originalBody, SecretKey);

            // Attacker changed amount to 100 kobo
            var tamperedBody = "{\"event\":\"charge.success\",\"data\":{\"reference\":\"REG-123\",\"amount\":100}}";

            var isValid = WebhookController.VerifyPaystackSignature(tamperedBody, validSignature, SecretKey);

            Assert.False(isValid);
        }

        [Fact]
        public void VerifyPaystackSignature_WithWrongSecretKey_ReturnsFalse()
        {
            var body = "{\"event\":\"charge.success\",\"data\":{\"reference\":\"REG-123\"}}";
            var signatureFromOtherKey = ComputeValidHmacHex(body, "different_secret_key");

            var isValid = WebhookController.VerifyPaystackSignature(body, signatureFromOtherKey, SecretKey);

            Assert.False(isValid);
        }

        [Fact]
        public void VerifyPaystackSignature_WithForgedOrRandomSignature_ReturnsFalse()
        {
            var body = "{\"event\":\"charge.success\"}";
            var forgedSignature = new string('a', 128); // 128 hex chars = 64 bytes

            var isValid = WebhookController.VerifyPaystackSignature(body, forgedSignature, SecretKey);

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-valid-hex-string!")]
        [InlineData("abc")] // wrong length
        public void VerifyPaystackSignature_WithMalformedSignature_ReturnsFalseWithoutThrowing(string? malformedSig)
        {
            var body = "{\"event\":\"charge.success\"}";

            var isValid = WebhookController.VerifyPaystackSignature(body, malformedSig, SecretKey);

            Assert.False(isValid);
        }
    }
}
