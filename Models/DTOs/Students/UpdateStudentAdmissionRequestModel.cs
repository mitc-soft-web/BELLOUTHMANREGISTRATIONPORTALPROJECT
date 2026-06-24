using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Models.DTOs.Students
{
    public class UpdateStudentAdmissionRequestModel
    {
        public Status Status { get; set; }
        public Guid DepartmentId { get; set; }
    }
}
