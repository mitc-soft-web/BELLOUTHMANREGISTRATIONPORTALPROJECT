using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;

namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class StudentRepository : BaseRepository, IStudentRepository
    {
        public StudentRepository(StudentDbContext studentContext) : base(studentContext) { }
        public async Task<Student> GetStudentByIdAsync(Guid studentId)
        {
            return await _studentContext.Set<Student>()
                           .Include(d => d.User)
                          .AsNoTracking()
                          .FirstOrDefaultAsync(std => std.Id == studentId);
        }
    }
}
