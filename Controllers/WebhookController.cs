using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs.Payments;

namespace Mitc_Registration_Portal.Controllers
{
    [ApiController]
    [Route("webhooks/paystack")]
    public class WebhookController(
        ILogger<WebhookController> logger,
        IConfiguration config,
        IOptions<PaystackConfiguration> paystackOptions,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IStudentService studentService) : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly PaystackConfiguration _paystack = paystackOptions?.Value ?? throw new ArgumentNullException(nameof(paystackOptions));
        private readonly IPaymentRepository _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IStudentService _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            // Paystack signature header: HMAC SHA512 over raw request body using your secret key.
            var signature = Request.Headers["x-paystack-signature"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogWarning("Paystack webhook rejected: missing x-paystack-signature header.");
                return Unauthorized();
            }

            Request.EnableBuffering();
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }
            Request.Body.Position = 0;

            var secretKey = _paystack.SecretKey ?? _config["Paystack:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogError("Paystack secret key is not configured for webhook verification.");
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
                _logger.LogWarning("Paystack webhook received with missing payment reference.");
                return Ok(); // Acknowledge to prevent unnecessary gateway retries
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
                        "Acknowledging unhandled Paystack webhook event {Event} for reference {Reference}.",
                        payload.Event,
                        payload.Data.Reference
                    );
                    break;
            }

            return Ok();
        }

        private async Task MarkPaymentSuccessfulAsync(PaystackWebhookPayload payload)
        {
            var reference = payload.Data?.Reference;
            if (string.IsNullOrWhiteSpace(reference))
            {
                _logger.LogWarning("Paystack charge.success missing reference.");
                return;
            }

            decimal? amountNaira = payload.Data.Amount.HasValue
                ? payload.Data.Amount.Value / 100m
                : null;

            var result = await _studentService.ProcessPaymentSuccessAndEnrollAsync(
                reference,
                amountNaira,
                payload.Data.PaidAt);

            if (!result.Status)
            {
                _logger.LogWarning(
                    "Webhook payment settlement result for reference {Reference}: {Message}",
                    reference,
                    result.Message);
            }
            else
            {
                _logger.LogInformation(
                    "Webhook payment settlement succeeded for reference {Reference}.",
                    reference);
            }
        }

        private async Task MarkPaymentFailedAsync(PaystackWebhookPayload payload)
        {
            var reference = payload.Data?.Reference;
            if (string.IsNullOrWhiteSpace(reference)) return;

            var payment = await _paymentRepository.GetByReference(reference);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for Paystack failed webhook reference {Reference}.", reference);
                return;
            }

            if (string.Equals(payment.PaymentStatus, "Successful", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Ignoring charge.failed for reference {Reference} as it is already marked Successful.", reference);
                return;
            }

            payment.PaymentStatus = "Failed";
            payment.PaidAt = payload.Data.PaidAt ?? payment.PaidAt;
            payment.ConcurrencyStamp = Guid.NewGuid();

            _paymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        public static bool VerifyPaystackSignature(string body, string? signature, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secretKey))
            {
                return false;
            }

            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            using var hmac = new HMACSHA512(keyBytes);
            var computedHash = hmac.ComputeHash(bodyBytes);

            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromHexString(signature.Trim());
            }
            catch (FormatException)
            {
                return false;
            }

            return signatureBytes.Length == computedHash.Length
                && CryptographicOperations.FixedTimeEquals(computedHash, signatureBytes);
        }
    }
}
