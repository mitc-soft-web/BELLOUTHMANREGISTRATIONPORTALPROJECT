using Mitc_Registration_Portal.Contracts.Entities;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class Department : BaseEntity
    {
        public string DepartmentName { get; set; }
        public string DepartmentalCode { get; set; }
        public ICollection<Student> Students { get; set; }
    }
}
