using Mitc_Registration_Portal.Contracts.Entities;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class Payment : BaseEntity
    {
        public Student? Student { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "Pending";
        public string? PaidAt { get; set; }
        public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
    }
}
