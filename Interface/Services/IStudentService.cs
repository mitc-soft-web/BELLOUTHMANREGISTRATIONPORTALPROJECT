using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Passports;
using Mitc_Registration_Portal.Models.DTOs.Payments;
using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.DTOs.Waecs;

namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IStudentService
    {
       void StepOneRegistration(CreateStudentRequestModel model);
       Task StepTwoRegistration(CreateNewWaecModel waecModel,CreatePassportRequestModel passportModel);
        Task<BaseResponse<PayStackInitDto>> InitiateRegistration();
        Task<BaseResponse<bool>> CreateAsync();
        Task<BaseResponse<bool>> VerifyPaymentAsync();
        Task<BaseResponse<IEnumerable<StudentDtos>>> StudentAsync();
    }
}
