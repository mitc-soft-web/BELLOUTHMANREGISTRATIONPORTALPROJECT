using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Models.DTOs.Waecs
{
    public class WaecDtos
    {
        public Guid StudentId { get; set; }
        public StudentDtos Student { get; set; }
        public string ExaminationYear { get; set; }
        public string ExaminationNumber { get; set; }
        public IFormFile WaecCertificate { get; set; }
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
