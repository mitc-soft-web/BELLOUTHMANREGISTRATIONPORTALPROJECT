using Microsoft.AspNetCore.Mvc;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs.Departments;

namespace Mitc_Registration_Portal.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var allDepartments = await _departmentService.GetAllDepartmentsAsync();
            if (allDepartments == null)
            {
                ViewBag.Message = "No data found";
            }

            return View(allDepartments);
        }
        public IActionResult GetAllDepartmentsAlongStudents()
        {
            var allDepartments = _departmentService.GetAllDepartmentsAndStudents();
            return View(allDepartments);
        }

        [HttpGet]
        public IActionResult CreateDepartment()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(CreateDepartmentRequestModel model)
        {
            await _departmentService.CreateAsync(model);

            return RedirectToAction(nameof(Index));
        }

        //[HttpGet]
        //public async Task<IActionResult> GetDepartment([FromRoute] int id)
        //{
        //    var department = await _departmentService.GetDepartmentById(id);
        //    if (department == null)
        //    {
        //        throw new Exception("Request not found!");
        //    }

        //    return View(department);
        //}

        //[HttpGet]
        //public async Task<IActionResult> EditDepartment(int id)
        //{

        //    var dept = await _departmentService.GetDepartmentById(id);
        //    if (dept == null)
        //    {
        //        throw new Exception("Request unsuccessful");
        //    }

        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> EditDepartment([FromRoute] int id, [FromBody] UpdateDepartmentRequestModel model)
        //{

        //    await _departmentService.UpdateDepartment(id, model);

        //    return RedirectToAction("Index");
        //}

        //[HttpGet]
        //public async Task<IActionResult> DeleteDepartment(Guid id)
        //{
        //    var getDepartment = await _departmentService.
        //}
        //[HttpDelete]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    await _departmentService.DeleteDepartment(id);
        //    return RedirectToAction("Index");
        //}
    }
}
