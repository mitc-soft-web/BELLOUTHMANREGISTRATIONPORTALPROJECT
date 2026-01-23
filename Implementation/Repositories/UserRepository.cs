using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;
namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class UserRepository : BaseRepository,IUserRepository
    {
        public UserRepository(StudentDbContext studentContext) : base(studentContext)
        {
        }

        public async Task<bool> Any(Expression<Func<User, bool>> expression)
        {
            return await _studentContext.Set<User>()
                .AnyAsync(expression);
        }

        public async Task<IReadOnlyList<User>> GetByRole(Expression<Func<User, bool>> expression)
        {
            return await _studentContext.Set<User>()
                .Where(expression)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<User> GetUserAndRoles(Guid userId)
        {
            return await _studentContext.Set<User>()
                 .Where(i => i.Id == userId)
                 .Include(u => u.UserRoles)
                 .ThenInclude(r => r.Role)
                 .SingleOrDefaultAsync();
        }
        public async Task<User> GetUserProfile(Guid userId)
        {
            return await _studentContext.Set<User>()
                .Where(u => u.Id == userId)
                .Include(a => a.Admin)
                .Include(p => p.Student)
                .ThenInclude(p => p.Department)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                 .AsSplitQuery()
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }
        public async Task<User> GetUserByEmail(string email)
        {
            return await _studentContext.Set<User>()
                .Include(u => u.UserRoles)
                .ThenInclude(r => r.Role)
                .Include(a => a.Admin)
                .Include(c => c.Student)
                .SingleOrDefaultAsync(u => u.Email == email);
        }
    }
}
