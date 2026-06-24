using Mitc_Registration_Portal.Models.DTOs.Passports;
using Mitc_Registration_Portal.Models.DTOs.Waecs;

namespace Mitc_Registration_Portal.Models.DTOs.Document
{
    public class DocumentViewModel
    {
        public CreateNewWaecModel waecModel { get; set; } = new();
        public CreatePassportRequestModel passportModel { get; set; } = new();
    }
}
