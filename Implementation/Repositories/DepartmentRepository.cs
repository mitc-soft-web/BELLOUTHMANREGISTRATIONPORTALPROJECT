using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;
namespace Mitc_Registration_Portal.Implementation.Repositories
{
    public class DepartmentRepository : BaseRepository, IDepartmentRepository
    {
        public DepartmentRepository(StudentDbContext studentContext) : base(studentContext)
        {
        }

        public async Task<bool> ExistByName(string departmentName)
        {
            return await _studentContext.Set<Department>().AnyAsync(dpt => dpt.DepartmentName == departmentName);
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAndStudents()
        {
            return await _studentContext.Set<Department>()
                         .Include(std => std.Students)
                         .AsNoTracking()
                         .ToListAsync();

        }

        public async Task<Department> GetDepartmentById(Guid id)
        {
            return await _studentContext.Set<Department>()
              .AsNoTracking()
              .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}
