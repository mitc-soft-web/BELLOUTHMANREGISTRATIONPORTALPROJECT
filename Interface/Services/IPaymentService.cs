using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Payments;

namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IPaymentService
    {
        Task<BaseResponse<PayStackInitDto>> IniatilizeRegistrationPayment(string email);
        Task<bool> VerifyPaymentAsync(string reference);
    }

}
