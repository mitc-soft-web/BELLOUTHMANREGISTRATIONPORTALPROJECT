using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Roles;

namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IRoleService
    {
        Task<BaseResponse<RoleDtos>> GetByIdAsync(Guid roleId, CancellationToken cancellationToken);
        Task<BaseResponse<IReadOnlyList<RoleDtos>>> GetAsync(string param, CancellationToken cancellationToken);
        Task<BaseResponse<IEnumerable<RoleDtos>>> GetRolesAsync(CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid roleId);
    }
}
