using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Interface.Repositories
{
    public interface IRegistrationDraftRepository : IBaseRepository
    {
        Task<RegistrationDraft?> GetByEmailAsync(string email);
        Task<RegistrationDraft?> GetByPaymentReferenceAsync(string reference);
        Task<RegistrationDraft?> GetByIdAsync(Guid id);
    }
}

