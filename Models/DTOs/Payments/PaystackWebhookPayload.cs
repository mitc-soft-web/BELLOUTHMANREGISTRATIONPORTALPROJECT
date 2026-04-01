using System.Text.Json.Serialization;

namespace Mitc_Registration_Portal.Models.DTOs.Payments
{
    public class PaystackWebhookPayload
    {
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("data")]
        public PaystackWebhookData? Data { get; set; }
    }

    public class PaystackWebhookData
    {
        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        // Amount is sent in kobo.
        [JsonPropertyName("amount")]
        public long? Amount { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        // ISO datetime string, e.g. "2020-01-01T00:00:00.000Z"
        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
