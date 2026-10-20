using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Departments;

namespace Mitc_Registration_Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentController : Controller
    {
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<DepartmentController> _logger;

        public DepartmentController(IDepartmentService departmentService, ILogger<DepartmentController> logger)
        {
            _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var allDepartments = await _departmentService.GetAllDepartmentsAsync();
                if (allDepartments == null)
                {
                    allDepartments = new BaseResponse<IEnumerable<DepartmentDtos>>
                    {
                        Status = true,
                        Message = "No departments found",
                        Data = new List<DepartmentDtos>()
                    };
                }
                else if (allDepartments.Data == null)
                {
                    allDepartments.Data = new List<DepartmentDtos>();
                }

                return View(allDepartments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load departments in DepartmentController.Index");
                return View(new BaseResponse<IEnumerable<DepartmentDtos>>
                {
                    Status = false,
                    Message = "Unable to load departments at this time.",
                    Data = new List<DepartmentDtos>()
                });
            }
        }

        [HttpGet]
        public IActionResult CreateDepartment()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(CreateDepartmentRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _departmentService.CreateAsync(model);
                if (result == null || !result.Status)
                {
                    ViewBag.ErrorMessage = result?.Message ?? "Failed to create department.";
                    return View(model);
                }

                TempData["SuccessMessage"] = result.Message ?? "Department created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                ViewBag.ErrorMessage = "An unexpected error occurred while creating the department.";
                return View(model);
            }
        }
    }
}
