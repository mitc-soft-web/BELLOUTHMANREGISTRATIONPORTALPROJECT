using Mitc_Registration_Portal.Contracts.Entities;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class Waec : BaseEntity
    {
        public Student Student { get; set; }
        public string ExaminationYear {  get; set; }
        public string ExaminationNumber { get; set; }

    }
}
