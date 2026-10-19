namespace Mitc_Registration_Portal.Configuration
{
    public static class PaystackConfigurationExtensions
    {
        public static PaystackConfiguration GetPaystack(this IConfiguration configuration)
        {
            var options = new PaystackConfiguration();
            configuration.GetSection(PaystackConfiguration.SectionName).Bind(options);
            return options;
        }

        public static bool MatchesExpectedFee(this PaystackConfiguration paystack, decimal? amountNaira)
        {
            if (!amountNaira.HasValue)
            {
                return false;
            }

            return amountNaira.Value == paystack.RegistrationFeeNaira;
        }

        public static bool MatchesExpectedFeeKobo(this PaystackConfiguration paystack, long? amountKobo)
        {
            if (!amountKobo.HasValue)
            {
                return false;
            }

            return amountKobo.Value == paystack.RegistrationFeeKobo;
        }
    }
}
