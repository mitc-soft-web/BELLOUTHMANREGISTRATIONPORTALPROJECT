using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Models.DTOs.Waecs
{
    public class WaecResultDetailsModel
    {
        public ResultSittingType SittingType { get; set; } = ResultSittingType.OneSitting;
        public string? ExaminationBody { get; set; }
        public string? SecondExaminationBody { get; set; }
        public string? SecondExaminationYear { get; set; }
        public string? SecondExaminationNumber { get; set; }
        public List<WaecSubjectResultEntryModel> SubjectResults { get; set; } = [];
    }
}
