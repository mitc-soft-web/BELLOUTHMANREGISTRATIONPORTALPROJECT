using Microsoft.Extensions.Configuration;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class ReCaptchaService : IReCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private readonly ILogger<ReCaptchaService> _logger;

        public ReCaptchaService(HttpClient httpClient, IConfiguration configuration,ILogger<ReCaptchaService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? new HttpClient();
            _secretKey = configuration?.GetSection("ReCaptchaSecretKey")?.Value ?? string.Empty;
        }

        public async Task<bool> IsCapchaValid(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("reCAPTCHA validation failed because the submitted token was empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_secretKey))
            {
                _logger.LogWarning("reCAPTCHA validation failed because ReCaptchaSecretKey is not configured.");
                return false;
            }

            var url = $"https://www.google.com/recaptcha/api/siteverify?secret={Uri.EscapeDataString(_secretKey)}&response={Uri.EscapeDataString(token)}";
            using var response = await _httpClient.PostAsync(url,null).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("reCAPTCHA verification request failed with status code {StatusCode}.", response.StatusCode);
                return false;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<RecaptchaResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result == null)
            {
                _logger.LogWarning("reCAPTCHA verification returned an empty or unreadable response.");
                return false;
            }

            if (!result.Success)
            {
                _logger.LogWarning("reCAPTCHA verification was not successful.");
                return false;
            }

            if (result.Score < 0.5)
            {
                _logger.LogWarning("Potential bot blocked by reCAPTCHA. Score: {Score}", result.Score);
                return false;
            }

            return true;
        }
        //try
        //{
        //    using var doc = JsonDocument.Parse(content);
        //    if (doc.RootElement.TryGetProperty("success", out var success))
        //        return success.GetBoolean();
        //}
        //catch (JsonException)
        //{
        //    // invalid json -> treat as invalid
        //}

        //return false;
    }
    
}
