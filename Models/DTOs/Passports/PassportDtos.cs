using Mitc_Registration_Portal.Models.DTOs.Students;

namespace Mitc_Registration_Portal.Models.DTOs.Passports
{
    public class PassportDtos
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public StudentDtos Student { get; set; }
        public DateTime DateCreated { get; set; }
        public IFormFile PassportPhoto { get; set; }
        public string PassportUrl { get; set; }
        public bool IsBackgroundWhite { get; set; }
    }
}
