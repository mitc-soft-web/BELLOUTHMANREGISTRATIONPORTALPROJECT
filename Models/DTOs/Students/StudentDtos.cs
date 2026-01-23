using Mitc_Registration_Portal.Models.DTOs.Departments;
using Mitc_Registration_Portal.Models.DTOs.Passports;
using Mitc_Registration_Portal.Models.DTOs.Payments;
using Mitc_Registration_Portal.Models.DTOs.Users;
using Mitc_Registration_Portal.Models.DTOs.Waecs;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Models.DTOs.Students
{
    public class StudentDtos
    {
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Gender? Gender { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
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
        //public Guid PaymentId { get; set; }
        //public PaymentDtos Payment { get; set; }
        //public Guid PassportId { get; set; }
        //public PassportDtos Passport { get; set; }
        //public Guid WaecId { get; set; }
        //public WaecDtos Waec { get; set; }
        //public Guid DepartmentId { get; set; }
        //public DepartmentDtos Department { get; set; }
        //public Guid UserId { get; set; }
        //public UserDtos User { get; set; }
    }
}
