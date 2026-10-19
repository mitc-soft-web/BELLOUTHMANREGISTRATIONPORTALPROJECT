using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Interface.Repositories
{
    public interface IPaymentRepository : IBaseRepository
    {
        Task<Payment?> GetByReference(string reference);
    }
}
