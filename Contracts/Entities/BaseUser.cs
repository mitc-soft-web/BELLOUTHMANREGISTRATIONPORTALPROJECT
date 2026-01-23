using System.Reflection;
using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Contracts.Entities
{
    public abstract class BaseUser : BaseEntity
    {
        public DateTime? DateOfBirth { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Gender? Gender { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }

        public string FullName()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
