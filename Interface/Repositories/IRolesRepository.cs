using System.Linq.Expressions;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Interface.Repositories
{
    public interface IRolesRepository
    {
        Task<IEnumerable<Role>> GetRolesByIdsAsync(Expression<Func<Role, bool>> expression);
        Task<IEnumerable<Role>> GetRoles();
    }
}
