using Mitc_Registration_Portal.Contracts.Entities;
using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Models.Entities
{
    // Stores in-progress registration data so payment callback can complete even if session expires.
    // Keep this lightweight and delete it on successful completion (or via TTL cleanup).
    public class RegistrationDraft : BaseEntity
    {
        public string Email { get; set; } = string.Empty;

        // Step 1
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public Gender? Gender { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string NIN { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public Guid DepartmentId { get; set; }

        // Store only the hash, never the raw password.
        public string PasswordHash { get; set; } = string.Empty;

        // Step 2
        public string ExamNo { get; set; } = string.Empty;
        public string ExamYear { get; set; } = string.Empty;
        public string CertificatesPath { get; set; } = string.Empty;
        public string PassportPath { get; set; } = string.Empty;

        // Payment
        public string? CurrentPaymentReference { get; set; }

        // State/TTL
        public string Status { get; set; } = "Draft";
        public DateTime DateUpdatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(2);
    }
}
