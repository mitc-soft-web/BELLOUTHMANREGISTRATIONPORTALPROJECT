using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Interface.Repositories
{
    public interface IStudentRepository : IBaseRepository
    {
        public Task<Student> GetStudentByIdAsync(Guid studentId);
    }
}
