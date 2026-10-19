using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;

namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class PaymentRepository : BaseRepository, IPaymentRepository
    {
        public PaymentRepository(StudentDbContext context) : base(context) { }

        public async Task<Payment?> GetByReference(string reference)
        {
            // Return a tracked entity with Student navigation because callers update and inspect it.
            return await _studentContext.Set<Payment>()
               .Include(p => p.Student)
               .FirstOrDefaultAsync(d => d.Reference == reference);
        }
    }
}
