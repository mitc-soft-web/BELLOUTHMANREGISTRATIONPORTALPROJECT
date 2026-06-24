using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public Guid DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? RejectionReason { get; set; }
        public Guid PassportId { get; set; }
        public string PassportUrl { get; set; }
        public Mitc_Registration_Portal.Models.DTOs.Waecs.CreateNewWaecModel? WaecDetails { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem>? DepartmentsSelectListItems { get; set; }
    }
}
