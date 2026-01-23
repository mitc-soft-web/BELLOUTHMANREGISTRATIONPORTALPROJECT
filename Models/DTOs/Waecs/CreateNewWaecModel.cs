namespace Mitc_Registration_Portal.Models.DTOs.Waecs
{
    public class CreateNewWaecModel
    {
        public string ExaminationYear { get; set; }
        public string ExaminationNumber { get; set; }
        public IFormFile WaecCertificate { get; set; }
    }
}
