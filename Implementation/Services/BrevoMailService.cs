using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mitc_Registration_Portal.Interface.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class BrevoMailService : IBrevoMailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrevoMailService> _logger;
        private readonly HttpClient _httpClient;

        public BrevoMailService(IConfiguration configuration, ILogger<BrevoMailService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var apiKey = _configuration["MITCAPIs:BrevoApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Brevo API Key is missing in appsettings.json under MITCAPIs:BrevoApiKey");
                return false;
            }

            var payload = new
            {
                sender = new { email = "buthman111@gmail.com", name = "MITC Admissions" },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = htmlContent
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            try
            {
                var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully sent email to {toEmail}");
                    return true;
                }
                
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send email to {toEmail}. Brevo Error: {errorResponse}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending email to {toEmail}");
                return false;
            }
        }
    }
}
