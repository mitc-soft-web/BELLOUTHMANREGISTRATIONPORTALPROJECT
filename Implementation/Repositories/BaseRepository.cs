using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Contracts.Entities;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Persistence.Context;

namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class BaseRepository(StudentDbContext studentContext) : IBaseRepository
    {
        protected readonly StudentDbContext _studentContext = studentContext ?? throw new ArgumentNullException(nameof(studentContext));
        public async Task<T> Add<T>(T entity) where T : BaseEntity
        {
            var entry = await _studentContext.Set<T>().AddAsync(entity);
            return entry.Entity;
        }


        public async Task<T> Get<T>(Expression<Func<T, bool>> expression) where T : BaseEntity
        {
            return await _studentContext.Set<T>().FirstOrDefaultAsync(expression);
        }
        public void Delete<T>(T entity) where T : BaseEntity
        {
            if (entity == null)return;
            var entry = _studentContext.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                _studentContext.Set<T>().Attach(entity);
            }

            _studentContext.Set<T>().Remove(entity);
        }

        public async Task<IReadOnlyList<T>> GetAll<T>() where T : BaseEntity
        {
            return await _studentContext.Set<T>()
                         .Where(i => i.IsDelete == false)
                         .ToListAsync();
        }

        public IQueryable<T> QueryWhere<T>(Expression<Func<T, bool>> expression) where T : BaseEntity
        {
            return _studentContext.Set<T>().Where(expression);
        }

        public void Update<T>(T entity) where T : BaseEntity
        {
            if (entity == null) return;

            // Ensure entity is attached and marked Modified so EF will persist property changes.
            var entry = _studentContext.Entry(entity);
            // Don't accidentally convert newly-added entities into UPDATEs (causes 0 rows affected -> concurrency exception).
            if (entry.State == EntityState.Added)
            {
                return;
            }
            if (entry.State == EntityState.Detached)
            {
                _studentContext.Set<T>().Attach(entity);
                entry.State = EntityState.Modified;
            }
            else
            {
                entry.State = EntityState.Modified;
            }
        }
    }
}
