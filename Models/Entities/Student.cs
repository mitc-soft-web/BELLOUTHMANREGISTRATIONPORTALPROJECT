using Mitc_Registration_Portal.Contracts.Entities;
using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class Student : BaseUser
    {
   
        public bool IsAdmitted { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public string NIN { get; set; }
        public string RegistrationNumber { get; set; }
        public Status Status { get; set; }  
        public string? MatricNumber { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? RejectionReason { get; set; }

        //Navigation properties
        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; }
        public Guid PassportId { get; set; }
        public Passport Passport { get; set; }
        public Guid WaecId { get; set; }
        public Waec Waec { get; set; }
        public Guid DepartmentId { get; set; }
        public Department Department { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
