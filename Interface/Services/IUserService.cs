using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Users;

namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IUserService
    {
        public Task<BaseResponse<LoginResponseModel>> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken);
        public Task<BaseResponse<UserDtos>> GetUserByEmail(string email, CancellationToken cancellationToken);
        //public Task<BaseResponse<bool>> DeleteAsync(Guid id);
        public Task<BaseResponse<UserDtos>> GetUserProfileByUserId(Guid userId, CancellationToken cancellationToken);
        //public Task<BaseResponse<int>> GetAllHospitalStaffs(CancellationToken cancellationToken);
    }
}
