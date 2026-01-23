using Mitc_Registration_Portal.Contracts.Entities;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public Student? Student { get; set; }

        public Admin? Admin { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = [];


        public string ChangePassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new ArgumentException("Password cannot be empty", nameof(newPassword));
            }
            PasswordHash = newPassword;
            return PasswordHash;
        }
    }
}
