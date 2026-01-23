using Microsoft.EntityFrameworkCore.Storage;

namespace Mitc_Registration_Portal.Interface.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
