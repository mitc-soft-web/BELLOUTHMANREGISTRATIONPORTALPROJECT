using Mitc_Registration_Portal.Models.DTOs.Roles;
using Mitc_Registration_Portal.Models.DTOs.Students;

namespace Mitc_Registration_Portal.Models.DTOs.Users
{
    public class UserDtos
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public ICollection<RoleDtos> Roles { get; set; } = [];
        public StudentDtos Student { get; set; }
        public AdminDto Admin { get; set; }
    }
}
