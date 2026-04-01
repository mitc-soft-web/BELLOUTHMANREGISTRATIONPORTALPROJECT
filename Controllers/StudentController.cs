using System.Security.Claims;
using ClosedXML.Excel;
using Findx.Common;
using Findx.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Mitc_Registration_Portal.Filters;
using Mitc_Registration_Portal.Implementation.Services;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs.Document;
using Mitc_Registration_Portal.Models.DTOs.Students;

namespace Mitc_Registration_Portal.Controllers
{
    public class StudentController(IStudentService studentService,IDepartmentService departmentService) : Controller
    {
        private readonly IStudentService _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
        private readonly IDepartmentService _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Index()
        {
            // Get studentId from the logged-in user's claims
            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine(User.FindFirstValue(ClaimTypes.GivenName));
            if (string.IsNullOrEmpty(studentIdClaim) || !Guid.TryParse(studentIdClaim, out Guid studentId))
            {
                return RedirectToAction("Login", "User");
            }

            var response = await _studentService.StudentDashboard(studentId, CancellationToken.None);

            if (!response.Status || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message ?? "Unable to load dashboard";
                return View();
            }

            return View(response);
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
        public async Task<IActionResult> StepOneRegistration(CreateStudentRequestModel model)
        {
            var postedDept = Request.Form["DepartmentId"].FirstOrDefault();
            Console.WriteLine("Posted DepartmentId (raw form): " + (postedDept ?? "<null>"));
            Console.WriteLine("Bound model.DepartmentId: " + (model?.DepartmentId.ToString() ?? "<null>"));

            if (!ModelState.IsValid)
            {
                //foreach (var key in ModelState.Keys)
                //{
                //    var state = ModelState[key];
                //    foreach (var error in state.Errors)
                //    {
                //        Console.WriteLine($"Field: {key} - error: {error.ErrorMessage}");
                //    }
                //}
                // Repopulate department select list before returning the view
                var departments = await _departmentService.GetAllDepartmentsAsync();
                model.DepartmentsSelectListItems = departments.Data?.Select(dpt => new SelectListItem
                {
                    Text = dpt.DepartmentName,
                    Value = dpt.Id.ToString()
                });
                return View(model);
            }
            var student = await _studentService.StepOneRegistration(model);
            return RedirectToAction("StepTwoRegistration");
        }
        [HttpGet]
        [RegistrationStepFilter("Email", "StepTwoRegistration")]
        public IActionResult StepTwoRegistration()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> StepTwoRegistration(DocumentViewModel model)
        {
            if (!TryValidateModel(model))
            {
                return View(model);
            }
            // Await the async service so files are saved and session keys are set before redirect
            await _studentService.StepTwoRegistration(model.waecModel, model.passportModel);
            return RedirectToAction("Index", "Payment");
        }
        [HttpGet]
        public async Task<IActionResult> GetAllStudent()
        {
            var getAllStudent = await _studentService.StudentAsync();
            return View(getAllStudent);
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            var getStudent = await _studentService.GetByIdAsync(id, CancellationToken.None);
            if (!getStudent.Status || getStudent.Data == null)
            {
                return NotFound();
            }
            return View(getStudent);
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit([FromRoute] Guid id)
        {
            var student = await _studentService.GetByIdAsync(id, CancellationToken.None);
            if (!student.Status || student.Data == null)
            {
                TempData["ErrorMessage"] = student.Message ?? "Unable to load student record.";
                return RedirectToAction(nameof(GetAllStudent));
            }
            return View(student);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] Guid id, [FromForm] UpdateStudentAdmissionRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                var student = await _studentService.GetByIdAsync(id, CancellationToken.None);
                if (student.Data != null)
                {
                    student.Data.Status = model.Status;
                }
                return View(student);
            }

            var response = await _studentService.UpdateAdmissionStatus(id, model);
            if (!response.Status)
            {
                TempData["ErrorMessage"] = response.Message ?? "Unable to update admission status.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            TempData["SuccessMessage"] = response.Message ?? "Admission status updated successfully.";
            return RedirectToAction(nameof(GetStudentById), new { id });
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var student = await _studentService.GetByIdAsync(id, CancellationToken.None);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStudent([FromRoute] Guid id)
        {
            var student = await _studentService.DeleteAsync(id, CancellationToken.None);
            if (!student.Status)
            {
                return NotFound();
            }
            return RedirectToAction("GetAllStudent", "Student");
        }


[HttpGet]
    public async Task<IActionResult> ExportStudentsToExcel()
    {
        // 1. Fetch the data (replace with your actual service call)
        var response = await _studentService.StudentAsync();
        var students = response.Data;

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Students List");
            var currentRow = 1;

            // 2. Create Header Row
            worksheet.Cell(currentRow, 1).Value = "First Name";
            worksheet.Cell(currentRow, 2).Value = "Last Name";
            worksheet.Cell(currentRow, 3).Value = "Email";
            worksheet.Cell(currentRow, 4).Value = "Gender";
            worksheet.Cell(currentRow, 5).Value = "Location";
            worksheet.Cell(currentRow, 6).Value = "Date Joined";

            // Style the header
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.BabyBlue;

            // 3. Fill Data Rows
            foreach (var student in students)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = student.FirstName ?? "N/A";
                worksheet.Cell(currentRow, 2).Value = student.LastName ?? "N/A";
                worksheet.Cell(currentRow, 3).Value = student.Email;
                worksheet.Cell(currentRow, 4).Value = student.Gender.ToString();
                worksheet.Cell(currentRow, 5).Value = student.State;
                worksheet.Cell(currentRow, 6).Value = student.DateCreated.ToString("yyyy-MM-dd");
            }

            worksheet.Columns().AdjustToContents();

            // 4. Return the file to the browser
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Student_Directory_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }
}
}
