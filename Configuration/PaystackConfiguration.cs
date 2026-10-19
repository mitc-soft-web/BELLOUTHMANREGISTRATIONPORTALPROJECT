namespace Mitc_Registration_Portal.Configuration
{
    public class PaystackConfiguration
    {
        public const string SectionName = "Paystack";

        public string? SecretKey { get; set; }

        public string? CallbackUrl { get; set; }

        /// <summary>Registration fee in Nigerian Naira (Paystack expects kobo at API boundary).</summary>
        public decimal RegistrationFeeNaira { get; set; } = 5000m;

        public int RegistrationFeeKobo =>
            (int)Math.Round(RegistrationFeeNaira * 100m, MidpointRounding.AwayFromZero);

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(SecretKey) && !string.IsNullOrWhiteSpace(CallbackUrl);
    }
}
