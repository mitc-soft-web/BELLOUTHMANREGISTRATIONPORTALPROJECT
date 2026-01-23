using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Models.DTOs.Payments
{
    public class PaymentDtos
    {
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
        public Guid StudentId { get; set; }
        public StudentDtos Student { get; set; }
        public decimal Amount { get; set; }
        public string PaymentReference { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentDate { get; set; }
    }
}
