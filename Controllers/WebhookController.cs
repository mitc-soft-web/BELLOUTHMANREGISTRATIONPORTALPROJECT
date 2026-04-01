using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Models.DTOs.Payments;

namespace Mitc_Registration_Portal.Controllers
{
    [ApiController]
    [Route("webhooks/paystack")]
    public class WebhookController(
        ILogger<WebhookController> logger,
        IConfiguration config,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork) : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IPaymentRepository _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            // Paystack signature header: HMAC SHA512 over raw request body using your secret key.
            var signature = Request.Headers["x-paystack-signature"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogWarning("Paystack webhook missing signature header.");
                return Unauthorized();
            }

            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }

            var secretKey = _config["Paystack:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogError("Paystack secret key is not configured (Paystack:SecretKey).");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            if (!VerifyPaystackSignature(body, signature, secretKey))
            {
                _logger.LogWarning("Paystack webhook signature verification failed.");
                return Unauthorized();
            }

            PaystackWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<PaystackWebhookPayload>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Paystack webhook payload.");
                return BadRequest();
            }

            if (payload?.Data?.Reference == null)
            {
                _logger.LogWarning("Paystack webhook missing payment reference.");
                return Ok(); // acknowledge to prevent retries
            }

            switch (payload.Event)
            {
                case "charge.success":
                    await MarkPaymentSuccessfulAsync(payload);
                    break;
                case "charge.failed":
                    await MarkPaymentFailedAsync(payload);
                    break;
                default:
                    _logger.LogInformation(
                        "Ignoring Paystack webhook event {Event} for ref {Reference}.",
                        payload.Event,
                        payload.Data.Reference
                    );
                    break;
            }

            return Ok();
        }

        private async Task MarkPaymentSuccessfulAsync(PaystackWebhookPayload payload)
        {
            var reference = payload.Data!.Reference!;
            var payment = await _paymentRepository.GetByReference(reference);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for Paystack webhook ref {Reference}.", reference);
                return;
            }

            // Idempotency: if already marked successful, do nothing.
            if (string.Equals(payment.PaymentStatus, "Successful", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            payment.PaymentStatus = "Successful";
            payment.PaidAt = payload.Data.PaidAt ?? payment.PaidAt;

            // Paystack amount is in kobo; store naira if present.
            if (payload.Data.Amount.HasValue)
            {
                payment.Amount = payload.Data.Amount.Value / 100m;
            }

            _paymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task MarkPaymentFailedAsync(PaystackWebhookPayload payload)
        {
            var reference = payload.Data!.Reference!;
            var payment = await _paymentRepository.GetByReference(reference);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for Paystack webhook ref {Reference}.", reference);
                return;
            }

            if (string.Equals(payment.PaymentStatus, "Successful", StringComparison.OrdinalIgnoreCase))
            {
                // Don't downgrade successful payments.
                return;
            }

            payment.PaymentStatus = "Failed";
            payment.PaidAt = payload.Data.PaidAt ?? payment.PaidAt;

            _paymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        private static bool VerifyPaystackSignature(string body, string signature, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var bodyBytes = Encoding.UTF8.GetBytes(body ?? string.Empty);

            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(bodyBytes);
            var computed = ConvertToHexLower(hash);

            return FixedTimeEquals(computed, signature.Trim());
        }

        private static string ConvertToHexLower(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            var aBytes = Encoding.UTF8.GetBytes(a.ToLowerInvariant());
            var bBytes = Encoding.UTF8.GetBytes(b.ToLowerInvariant());
            return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }
    }
}
