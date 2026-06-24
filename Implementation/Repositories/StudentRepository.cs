using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;

namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class StudentRepository : BaseRepository, IStudentRepository
    {
        public StudentRepository(StudentDbContext studentContext) : base(studentContext) { }

        public async Task<bool> DeleteStudentById(Guid studentId)
        {
            var getStudent = await _studentContext.FindAsync<Student>(studentId);
            getStudent.IsDelete = true;
            _studentContext.Update(getStudent);
            return true;
        }

        public async Task<Student> GetStudentByIdAsync(Guid studentId)
        {
            return await _studentContext.Set<Student>()
                           .Include(d => d.User)
                           .Include(d => d.Passport)
                           .Include(d => d.Department)
                           .AsNoTracking()
                           .FirstOrDefaultAsync(std => std.UserId == studentId);
        }

        public async Task<Student> GetStudentByStudentId(Guid studentId)
        {
            return await _studentContext.Set<Student>()
                           .Include(d => d.Passport)
                           .Include(d => d.Department)
                           .Include(d => d.Waec)
                          .AsNoTracking()
                          .FirstOrDefaultAsync(std => std.Id == studentId);
        }

        public async Task<Student?> GetStudentForAdmissionUpdateAsync(Guid studentId)
        {
            return await _studentContext.Set<Student>()
                .Include(d => d.Department)
                .Include(d => d.User)
                .FirstOrDefaultAsync(std => std.Id == studentId);
        }
    }
}
