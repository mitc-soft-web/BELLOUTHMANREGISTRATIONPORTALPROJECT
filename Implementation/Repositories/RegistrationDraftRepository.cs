using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;

namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class RegistrationDraftRepository : BaseRepository, IRegistrationDraftRepository
    {
        public RegistrationDraftRepository(StudentDbContext context) : base(context) { }

        public Task<RegistrationDraft?> GetByEmailAsync(string email)
        {
            return _studentContext.Set<RegistrationDraft>()
                .FirstOrDefaultAsync(d => d.Email == email);
        }

        public Task<RegistrationDraft?> GetByPaymentReferenceAsync(string reference)
        {
            return _studentContext.Set<RegistrationDraft>()
                .FirstOrDefaultAsync(d => d.CurrentPaymentReference == reference);
        }

        public Task<RegistrationDraft?> GetByIdAsync(Guid id)
        {
            return _studentContext.Set<RegistrationDraft>()
                .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}

