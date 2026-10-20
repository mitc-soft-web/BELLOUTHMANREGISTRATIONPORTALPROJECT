using Microsoft.AspNetCore.Mvc.Rendering;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Departments;
using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class DepartmentService(IDepartmentRepository departmentRepository,ILogger<DepartmentService> logger,Interface.Repositories.IUnitOfWork unitOfWork) : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepository = departmentRepository ?? throw new ArgumentNullException(nameof(departmentRepository));
        private readonly ILogger<DepartmentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly Interface.Repositories.IUnitOfWork _unitOfWOrk = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        public async Task<BaseResponse<bool>> CreateAsync(CreateDepartmentRequestModel model)
        {
            var departmentExists = await _departmentRepository.ExistByName(model.DepartmentName);
            if (departmentExists)
            {
                _logger.LogError("Department aleady exists");
                return new BaseResponse<bool>
                {
                    Message = $"Department with name {model.DepartmentName} already exists",
                    Status = false
                };
            }

            var newDepartment = new Department
            {
                DepartmentName = model.DepartmentName,
                DepartmentalCode = model.DepartmentalCode,
                DateCreated =DateTime.UtcNow
            };
            var createDepartment = await _departmentRepository.Add<Department>(newDepartment);
            await _unitOfWOrk.SaveChangesAsync();
            if (createDepartment == null)
            {
                _logger.LogError("Create Department Failed");
                return new BaseResponse<bool>
                {
                    Message = "Create Department Failed",
                    Status = false
                };
            }
            _logger.LogInformation("Create Department Success");
            return new BaseResponse<bool>
            {
                Message = "Crate Department Success",
                Status = true,

            };
        }

        public async Task<BaseResponse<List<DepartmentDtos>>> GetAllDepartmentsAndStudents()
        {
            var getAllDepartmentsAndStudents = await _departmentRepository.GetAllDepartmentsAndStudents();
            if (!getAllDepartmentsAndStudents.Any())
            {
                _logger.LogError($"No department found");
                return new BaseResponse<List<DepartmentDtos>>
                {
                    Message = $"No department found",
                    Status = false,
                };
            }
            _logger.LogInformation("All departments along their students fetched successfully");
            return new BaseResponse<List<DepartmentDtos>>
            {
                Message = "All departments along their students fetched successfully",
                Status = true,
                Data = getAllDepartmentsAndStudents.Select(dpt => new DepartmentDtos
                {
                    Id = dpt.Id,
                    DepartmentName = dpt.DepartmentName,
                    DepartmentalCode = dpt.DepartmentalCode,
                    DateCreated = dpt.DateCreated,
                    Students = dpt.Students.Select(std => new StudentDtos()
                    {
                        Id = std.Id,
                        FirstName = std.FirstName,
                        LastName = std.LastName,
                        Email = std.Email,
                        PhoneNumber = std.PhoneNumber,
                        DateCreated = std.DateCreated,
                        Address = std.Address,
                        DateOfBirth = std.DateOfBirth,
                        State = std.State,
                        Status = std.Status,
                        MiddleName = std.MiddleName,
                        NIN =std.NIN,
                        RegistrationNumber = std.RegistrationNumber,
                        MatricNumber = std.MatricNumber,
                        Gender = std.Gender,
                    }).ToList()

                }).ToList()
            };
        }
        public async Task<BaseResponse<IEnumerable<DepartmentDtos>>> GetAllDepartmentsAsync()
        {
            try
            {
                var getAllDepartments = await _departmentRepository.GetAll<Department>();
                if (getAllDepartments == null || !getAllDepartments.Any())
                {
                    _logger.LogInformation("No departments found in database");
                    return new BaseResponse<IEnumerable<DepartmentDtos>>
                    {
                        Message = "No department found",
                        Status = true,
                        Data = new List<DepartmentDtos>()
                    };
                }

                _logger.LogInformation("All departments fetched successfully");
                return new BaseResponse<IEnumerable<DepartmentDtos>>
                {
                    Message = "All departments fetched successfully",
                    Status = true,
                    Data = getAllDepartments.Select(dpt => new DepartmentDtos
                    {
                        Id = dpt.Id,
                        DepartmentName = dpt.DepartmentName,
                        DepartmentalCode = dpt.DepartmentalCode,
                        DateCreated = dpt.DateCreated,
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching departments");
                return new BaseResponse<IEnumerable<DepartmentDtos>>
                {
                    Message = "An error occurred while fetching departments.",
                    Status = false,
                    Data = new List<DepartmentDtos>()
                };
            }
        }

        public async Task<BaseResponse<DepartmentDtos>> GetDepartmentById(Guid id)
        {
            var getDepartment = await _departmentRepository.GetDepartmentById(id);
            if(getDepartment == null)
            {
                _logger.LogError("Department Not found");
                return new BaseResponse<DepartmentDtos>
                {
                    Status = false,
                    Message = "Department not found"
                };
            }
            return new BaseResponse<DepartmentDtos>
            {
                Status = true,
                Message = "Department Found",
                Data = new DepartmentDtos
                {
                    DepartmentName = getDepartment.DepartmentName,
                    DepartmentalCode = getDepartment.DepartmentalCode,
                    DateCreated = getDepartment.DateCreated
                }
            };
        }

        public async Task<IEnumerable<SelectListItem>> GetDepartmentsSelectList()
        {
            var depts = await _departmentRepository.GetAll<Department>();
            return depts.Select(dpt => new SelectListItem
            {
                Text = dpt.DepartmentName,
                Value = dpt.Id.ToString()
            }).ToList();
        }
    }
}
