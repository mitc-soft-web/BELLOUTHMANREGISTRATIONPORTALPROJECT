using Mitc_Registration_Portal.Contracts.Entities;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class Passport : BaseEntity
    {
        public Student Student { get; set; }
        public string PassportUrl {  get; set; }
        public bool IsBackgroundWhite {  get; set; }

    }
}
