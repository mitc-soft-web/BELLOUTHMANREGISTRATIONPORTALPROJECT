using Mitc_Registration_Portal.Models.DTOs.Roles;
using Mitc_Registration_Portal.Models.DTOs.Students;

namespace Mitc_Registration_Portal.Models.DTOs.Users
{
    public class LoginRequestModel
    {

        public string Email { get; set; }
        public string Password { get; set; }
    }
        public class LoginResponseModel : BaseResponse
        {
            public string FirstName { get; set; }
            public string FullName { get; set; }
            public Guid UserId { get; set; }
            public string Email { get; set; }
            public IEnumerable<RoleDtos> Roles { get; set; } = new List<RoleDtos>();

            public StudentDtos Student { get; set; }
            public AdminDto Admin { get; set; }
        }
    }
