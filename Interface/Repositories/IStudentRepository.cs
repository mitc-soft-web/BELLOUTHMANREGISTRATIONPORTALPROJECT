using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Interface.Repositories
{
    public interface IStudentRepository : IBaseRepository
    {
        public Task<Student> GetStudentByIdAsync(Guid studentId);
        public Task<Student> GetStudentByStudentId(Guid studentId);
        public Task<Student?> GetStudentForAdmissionUpdateAsync(Guid studentId);
        public Task<bool> DeleteStudentById(Guid studentId);
    }
}
