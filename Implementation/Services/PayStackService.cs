using System.Net.Http.Headers;
using System.Text.Json;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Payments;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class PayStackService : IPayStackService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;


        public PayStackService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _config["Paystack:SecretKey"]
                );
        }

        public async Task<BaseResponse<PayStackInitDto>> Initialize(string reference, decimal amount, string email)
        {
            var payload = new
            {
                email = email,
                amount = (int)(amount * 100), // kobo
                reference = reference,
                callback_url = _config["Paystack:CallbackUrl"]
            };

            var response = await _httpClient.PostAsJsonAsync("transaction/initialize", payload);

            // If not successful, read the response body and return it so you can see Paystack's error message.
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return new BaseResponse<PayStackInitDto>
                {
                    Status = false,
                    Message = $"Paystack initialize failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {err}"
                };
            }

            // Paystack returns an envelope { status, message, data: { authorization_url, reference, ... } }
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var dataElement))
            {
                return new BaseResponse<PayStackInitDto>
                {
                    Status = false,
                    Message = "Unexpected Paystack response: missing data object."
                };
            }

            var authUrl = dataElement.GetProperty("authorization_url").GetString();
            var returnedReference = dataElement.TryGetProperty("reference", out var refEl) ? refEl.GetString() : reference;

            return new BaseResponse<PayStackInitDto>
            {
                Status = true,
                Message = "Payment Initialized",
                Data = new PayStackInitDto
                {
                    AuthorizationUrl = authUrl,
                    Reference = returnedReference ?? reference
                }

            };
        }

        public async Task<BaseResponse<PayStackVerifyDto>> Verify(string reference)
        {
            var response = await _httpClient.GetAsync($"transaction/verify/{reference}");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return new BaseResponse<PayStackVerifyDto>
                {
                    Status = false,
                    Message = $"Paystack verify failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {err}"
                };
            }

            // Parse verify envelope -> data -> status, channel, paid_at  (adjust if Paystack structure differs)
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var dataEl))
            {
                return new BaseResponse<PayStackVerifyDto>
                {
                    Status = false,
                    Message = "Unexpected Paystack verify response: missing data object."
                };
            }

            var status = dataEl.TryGetProperty("status", out var s) ? s.GetString() : null;
            var channel = dataEl.TryGetProperty("channel", out var c) ? c.GetString() : null;
            var paidAt = dataEl.TryGetProperty("paid_at", out var p) ? p.GetString() : null;

            return new BaseResponse<PayStackVerifyDto>
            {
                Status = string.Equals(status, "success", StringComparison.OrdinalIgnoreCase),
                Message = "Payment Verified",
                Data = new PayStackVerifyDto
                {
                    Status = status,
                    Channel = channel,
                    PaidAt = paidAt
                }
            };
        }

    }
}
