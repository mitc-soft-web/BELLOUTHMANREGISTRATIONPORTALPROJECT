using Mitc_Registration_Portal.Contracts.Entities;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class Payment : BaseEntity
    {
        public Student Student { get; set; }
        public decimal Amount {  get; set; }
        public string Reference { get; set; }
        public string PaymentStatus { get; set; }
        public string? PaidAt { get; set; }

    }
}
