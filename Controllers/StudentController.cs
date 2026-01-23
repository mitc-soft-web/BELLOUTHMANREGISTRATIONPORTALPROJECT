using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Mitc_Registration_Portal.Filters;
using Mitc_Registration_Portal.Implementation.Services;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs.Document;
using Mitc_Registration_Portal.Models.DTOs.Passports;
using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.DTOs.Waecs;

namespace Mitc_Registration_Portal.Controllers
{
    public class StudentController(IStudentService studentService,IDepartmentService departmentService) : Controller
    {
        private readonly IStudentService _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
        private readonly IDepartmentService _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        [Authorize(Roles = "Student")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> StepOneRegistration()
        {
            var departments = await _departmentService.GetAllDepartmentsAsync();
            CreateStudentRequestModel request = new CreateStudentRequestModel()
            {
                DepartmentsSelectListItems = departments.Data?.Select(dpt => new SelectListItem
                {
                   
                    Text = dpt.DepartmentName,
                    Value = dpt.Id.ToString()
                })
            };

            return View(request);
        }
        [HttpPost]
        public IActionResult StepOneRegistration(CreateStudentRequestModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View(model);
            //}
            _studentService.StepOneRegistration(model);
            return RedirectToAction("StepTwoRegistration");
        }
        [HttpGet]
        [RegistrationStepFilter("Email", "StepTwoRegistration")]
        public IActionResult StepTwoRegistration()
        {
            return View();
        }
        [HttpPost]
        public IActionResult StepTwoRegistration(DocumentViewModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View(model);
            //}
            _studentService.StepTwoRegistration(model.waecModel, model.passportModel);
            return RedirectToAction("Index", "Payment");

        }
        [HttpGet]
        public async Task<IActionResult> GetAllStudent()
        {
            var getAllStudent = await _studentService.StudentAsync();
            return View(getAllStudent);
        }

    }
}
