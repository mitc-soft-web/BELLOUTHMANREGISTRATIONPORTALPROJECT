using Mitc_Registration_Portal.Contracts.Entities;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class Admin : BaseUser
    {
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
