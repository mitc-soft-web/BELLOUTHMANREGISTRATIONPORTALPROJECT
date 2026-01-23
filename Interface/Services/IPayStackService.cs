using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Payments;

namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IPayStackService
    {
       
            Task<BaseResponse<PayStackInitDto>> Initialize(string reference, decimal amount,string email);
            Task<BaseResponse<PayStackVerifyDto>> Verify(string reference);

    }
}
