using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Roles;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRolesRepository _roleRepository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IRolesRepository roleRepository, ILogger<RoleService> logger)
        {
            _logger = logger;
            _roleRepository = roleRepository;
        }
        public Task<bool> DeleteAsync(Guid roleId)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<IReadOnlyList<RoleDtos>>> GetAsync(string param, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<RoleDtos>> GetByIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<IEnumerable<RoleDtos>>> GetRolesAsync(CancellationToken cancellationToken)
        {
            var roles = await _roleRepository.GetRoles();
            if (!roles.Any())
            {
                _logger.LogError("No roles found");
                return new BaseResponse<IEnumerable<RoleDtos>>
                {
                    Message = "No roles found",
                    Status = false
                };
            }

            _logger.LogInformation("Roles fetched successfully");
            return new BaseResponse<IEnumerable<RoleDtos>>
            {
                Message = "Roles fetched successfully",
                Status = true,
                Data = roles.Select(r => new RoleDtos
                {
                    Id = r.Id,
                    Name = r.Name,
                    DateCreated = r.DateCreated,
                })
            };
        }
    }
}
