using Microsoft.AspNetCore.Mvc.Rendering;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Departments;

namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IDepartmentService
    {
        Task<BaseResponse<bool>> CreateAsync(CreateDepartmentRequestModel model);
        Task<BaseResponse<List<DepartmentDtos>>> GetAllDepartmentsAndStudents();
        Task<BaseResponse<IEnumerable<DepartmentDtos>>> GetAllDepartmentsAsync();
        Task<IEnumerable<SelectListItem>> GetDepartmentsSelectList();
        Task<BaseResponse<DepartmentDtos>> GetDepartmentById(Guid id);
    }
}
