using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Payments;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class PayStackService : IPayStackService
    {
        private readonly HttpClient _httpClient;
        private readonly PaystackConfiguration _paystack;
        private readonly ILogger<PayStackService> _logger;

        public PayStackService(
            HttpClient httpClient,
            IOptions<PaystackConfiguration> paystackOptions,
            ILogger<PayStackService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _paystack = paystackOptions?.Value ?? throw new ArgumentNullException(nameof(paystackOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
            }

            if (!string.IsNullOrWhiteSpace(_paystack.SecretKey) && _httpClient.DefaultRequestHeaders.Authorization == null)
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _paystack.SecretKey);
            }
        }

        public async Task<BaseResponse<PayStackInitDto>> Initialize(string reference, decimal amount, string email)
        {
            if (!_paystack.IsConfigured)
            {
                _logger.LogError("Paystack configuration is incomplete (SecretKey or CallbackUrl missing).");
                return ConfigError<PayStackInitDto>(
                    "Paystack is not configured. Set Paystack:SecretKey and Paystack:CallbackUrl for this environment.");
            }

            if (amount != _paystack.RegistrationFeeNaira)
            {
                _logger.LogWarning("Initialize payment amount {Amount} differs from configured fee {ConfiguredFee}", amount, _paystack.RegistrationFeeNaira);
                return new BaseResponse<PayStackInitDto>
                {
                    Status = false,
                    Message = "Payment amount does not match the configured registration fee."
                };
            }

            try
            {
                var payload = new
                {
                    email = email,
                    amount = _paystack.RegistrationFeeKobo,
                    reference = reference,
                    callback_url = _paystack.CallbackUrl
                };

                var response = await _httpClient.PostAsJsonAsync("transaction/initialize", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Paystack initialize returned HTTP {StatusCode}: {ErrorBody}", response.StatusCode, err);
                    return new BaseResponse<PayStackInitDto>
                    {
                        Status = false,
                        Message = MapHttpError(response.StatusCode, "Payment initialization was rejected by the gateway.")
                    };
                }

                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var dataElement))
                {
                    _logger.LogWarning("Paystack initialize returned JSON without data object.");
                    return new BaseResponse<PayStackInitDto>
                    {
                        Status = false,
                        Message = "Unexpected Paystack response: missing data object."
                    };
                }

                var authUrl = dataElement.TryGetProperty("authorization_url", out var authUrlProp)
                    ? authUrlProp.GetString()
                    : null;
                var returnedReference = dataElement.TryGetProperty("reference", out var refEl)
                    ? refEl.GetString()
                    : reference;

                if (!IsValidPaymentUrl(authUrl))
                {
                    _logger.LogWarning("Paystack returned an invalid or untrusted authorization URL: {AuthUrl}", authUrl);
                    return new BaseResponse<PayStackInitDto>
                    {
                        Status = false,
                        Message = "Paystack did not return a valid payment page. Please check your connection and try again."
                    };
                }

                return new BaseResponse<PayStackInitDto>
                {
                    Status = true,
                    Message = "Payment Initialized",
                    Data = new PayStackInitDto
                    {
                        AuthorizationUrl = authUrl!,
                        Reference = returnedReference ?? reference
                    }
                };
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Paystack circuit breaker tripped (OPEN) during initialize.");
                return new BaseResponse<PayStackInitDto>
                {
                    Status = false,
                    Message = "Payment service is temporarily unavailable due to high error rates at the gateway. Please try again shortly."
                };
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogWarning(ex, "Resilience attempt/request timeout during Paystack initialize.");
                return NetworkError<PayStackInitDto>("Connection to Paystack timed out. Please check your internet connection and try again.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network connectivity failure during Paystack initialize.");
                return NetworkError<PayStackInitDto>("Could not connect to Paystack. Please check your internet connection and try again.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Task canceled/timeout during Paystack initialize.");
                return NetworkError<PayStackInitDto>("Connection to Paystack timed out. Please check your internet connection and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Paystack initialize.");
                return new BaseResponse<PayStackInitDto>
                {
                    Status = false,
                    Message = $"An error occurred while contacting Paystack: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponse<PayStackVerifyDto>> Verify(string reference)
        {
            if (!_paystack.IsConfigured)
            {
                _logger.LogError("Paystack configuration is incomplete (SecretKey or CallbackUrl missing).");
                return ConfigError<PayStackVerifyDto>(
                    "Paystack is not configured. Set Paystack:SecretKey and Paystack:CallbackUrl for this environment.");
            }

            try
            {
                var response = await _httpClient.GetAsync($"transaction/verify/{Uri.EscapeDataString(reference)}");
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Paystack verify returned HTTP {StatusCode} for ref {Reference}: {ErrorBody}", response.StatusCode, reference, err);
                    return new BaseResponse<PayStackVerifyDto>
                    {
                        Status = false,
                        Message = MapHttpError(response.StatusCode, "Payment verification was rejected by the gateway.")
                    };
                }

                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var dataEl))
                {
                    _logger.LogWarning("Paystack verify returned JSON without data object for ref {Reference}", reference);
                    return new BaseResponse<PayStackVerifyDto>
                    {
                        Status = false,
                        Message = "Unexpected Paystack verify response: missing data object."
                    };
                }

                var status = dataEl.TryGetProperty("status", out var s) ? s.GetString() : null;
                var channel = dataEl.TryGetProperty("channel", out var c) ? c.GetString() : null;
                var paidAt = dataEl.TryGetProperty("paid_at", out var p) ? p.GetString() : null;
                long? amountKobo = null;
                if (dataEl.TryGetProperty("amount", out var amountEl) && amountEl.TryGetInt64(out var kobo))
                {
                    amountKobo = kobo;
                }

                var paystackSuccess = string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);
                var amountOk = _paystack.MatchesExpectedFeeKobo(amountKobo);

                if (paystackSuccess && !amountOk)
                {
                    _logger.LogWarning("Paystack verify ref {Reference} amount mismatch: received {Kobo} kobo, expected {ExpectedKobo} kobo",
                        reference, amountKobo, _paystack.RegistrationFeeKobo);
                }

                return new BaseResponse<PayStackVerifyDto>
                {
                    Status = paystackSuccess && amountOk,
                    Message = paystackSuccess && !amountOk
                        ? "Payment amount did not match the required registration fee."
                        : paystackSuccess
                            ? "Payment Verified"
                            : "Payment was not successful.",
                    Data = new PayStackVerifyDto
                    {
                        Status = status,
                        Channel = channel,
                        PaidAt = paidAt,
                        AmountKobo = amountKobo
                    }
                };
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Paystack circuit breaker tripped (OPEN) during verify for ref {Reference}", reference);
                return new BaseResponse<PayStackVerifyDto>
                {
                    Status = false,
                    Message = "Payment service is temporarily unavailable due to high error rates at the gateway. Please try again shortly."
                };
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogWarning(ex, "Resilience timeout during Paystack verify for ref {Reference}", reference);
                return NetworkError<PayStackVerifyDto>("Connection to Paystack timed out. Please check your internet connection and try again.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network connectivity failure during Paystack verify for ref {Reference}", reference);
                return NetworkError<PayStackVerifyDto>("Could not reach Paystack. Check your internet connection and try again.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Task canceled/timeout during Paystack verify for ref {Reference}", reference);
                return NetworkError<PayStackVerifyDto>("Connection to Paystack timed out. Please check your internet connection and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Paystack verify for ref {Reference}", reference);
                return new BaseResponse<PayStackVerifyDto>
                {
                    Status = false,
                    Message = $"An error occurred while verifying the payment with Paystack: {ex.Message}"
                };
            }
        }

        private static string MapHttpError(HttpStatusCode statusCode, string fallback) => statusCode switch
        {
            HttpStatusCode.Unauthorized => "Payment gateway authentication failed. Please contact administrator.",
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout =>
                "Paystack payment gateway is temporarily unreachable. Please try again shortly.",
            _ => fallback
        };

        private static BaseResponse<T> ConfigError<T>(string message) =>
            new() { Status = false, Message = message };

        private static BaseResponse<T> NetworkError<T>(string message = "Could not reach Paystack. Check your internet connection and try again.") =>
            new()
            {
                Status = false,
                Message = message
            };

        private static bool IsValidPaymentUrl(string? authUrl)
        {
            if (string.IsNullOrWhiteSpace(authUrl)) return false;

            if (!Uri.TryCreate(authUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                return false;
            }

            return !uri.IsLoopback
                && !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
}
