
using Microsoft.EntityFrameworkCore.Storage;
using Mitc_Registration_Portal.Persistence.Context;
using IUnitOfWork = Mitc_Registration_Portal.Interface.Repositories.IUnitOfWork;

namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class UnitOfWork(StudentDbContext context) : IUnitOfWork
    {
        protected readonly StudentDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
