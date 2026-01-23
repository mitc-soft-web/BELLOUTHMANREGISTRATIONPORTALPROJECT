using Mitc_Registration_Portal.Contracts.Entities;

namespace Mitc_Registration_Portal.Models.Entities
{
    public class Waec : BaseEntity
    {
        public Guid StudentId { get; set; }
        public Student Student { get; set; }
        public string ExaminationYear {  get; set; }
        public string ExaminationNumber { get; set; }

    }
}
