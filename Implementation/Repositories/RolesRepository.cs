using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;

namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class RolesRepository : BaseRepository,IRolesRepository
    {
        public RolesRepository(StudentDbContext studentContext) : base(studentContext)
        {
            
        }

        public async Task<IEnumerable<Role>> GetRoles()
        {
            return await _studentContext.Set<Role>()
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Role>> GetRolesByIdsAsync(Expression<Func<Role, bool>> expression)
        {
            return await _studentContext.Set<Role>()
                 .Where(expression)
                 .AsNoTracking()
                 .ToListAsync();
        }
    }
}
