using System.Linq.Expressions;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Interface.Repositories
{
    public interface IUserRepository : IBaseRepository
    {
        public Task<IReadOnlyList<User>> GetByRole(Expression<Func<User, bool>> expression);
        public Task<User> GetUserAndRoles(Guid userId);
        Task<User> GetUserByEmail(string email);
        Task<User> GetUserProfile(Guid userId);
        Task<bool> Any(Expression<Func<User, bool>> expression);
    }
}
