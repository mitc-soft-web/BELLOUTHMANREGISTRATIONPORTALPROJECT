using Microsoft.AspNetCore.Mvc.Rendering;
using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Models.DTOs.Students
{
    public class CreateStudentRequestModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public string NIN { get; set; }
        public Guid DepartmentId { get; set; }
        public IEnumerable<SelectListItem> DepartmentsSelectListItems { get; set; }
    }
}
