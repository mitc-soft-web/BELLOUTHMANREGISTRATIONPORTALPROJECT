using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Passports;
using Mitc_Registration_Portal.Models.DTOs.Payments;
using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.DTOs.Waecs;

namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IStudentService
    {
        Task<BaseResponse<bool>> StepOneRegistration(CreateStudentRequestModel model);
        Task StepTwoRegistration(CreateNewWaecModel waecModel, CreatePassportRequestModel passportModel);
        Task<BaseResponse<PayStackInitDto>> InitiateRegistration();
        Task<BaseResponse<bool>> CreateAsync();

        // Reference is optional: Payment callback can supply it even if session is missing.
        Task<BaseResponse<bool>> VerifyPaymentAsync(string? reference = null);

        Task<BaseResponse<StudentDtos>> GetByIdAsync(Guid studentId, CancellationToken cancellationToken);
        Task<BaseResponse<IEnumerable<StudentDtos>>> StudentAsync();
        Task<BaseResponse<bool>> DeleteAsync(Guid studentId, CancellationToken cancellationToken);
        Task<BaseResponse<StudentDashboardDtos>> StudentDashboard(Guid studentId, CancellationToken cancellationToken);
        Task<BaseResponse<bool>> UpdateAdmissionStatus(Guid id, UpdateStudentAdmissionRequestModel model);
    }
}

