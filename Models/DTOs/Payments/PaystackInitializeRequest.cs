namespace Mitc_Registration_Portal.Models.DTOs.Payments
{
    public class PaystackInitializeRequest
    {
        public string Email { get; set; }
        public int AmountInKobo { get; set; }
        public string Reference { get; set; }
        public string CallBackUrl { get; set; }
    }
}
