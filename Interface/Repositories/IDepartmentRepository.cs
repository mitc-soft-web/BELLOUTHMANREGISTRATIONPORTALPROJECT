using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Interface.Repositories
{
    public interface IDepartmentRepository : IBaseRepository
    {
        Task<IEnumerable<Department>> GetAllDepartmentsAndStudents();
        Task<bool> ExistByName(string departmentName);
        Task<Department> GetDepartmentById(Guid id);
    }
}
