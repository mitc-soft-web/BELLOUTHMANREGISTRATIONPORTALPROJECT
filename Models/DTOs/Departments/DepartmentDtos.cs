using MassTransit;
using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Models.DTOs.Departments
{
    public class DepartmentDtos
    {
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
        public string DepartmentName { get; set; }
        public string DepartmentalCode { get; set; }
        public ICollection<StudentDtos> Students { get; set; }
    }
}
