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
using Mitc_Registration_Portal.Models.DTOs.Passports;
using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.DTOs.Waecs;
using Mitc_Registration_Portal.Models.Enums;

using Mitc_Registration_Portal.Contracts.Messaging;

namespace Mitc_Registration_Portal.Controllers
{
    public class StudentController(IReCaptchaService captchaService,IStudentService studentService,IDepartmentService departmentService, Mitc_Registration_Portal.Interface.Repositories.IRegistrationDraftRepository draftRepository, Mitc_Registration_Portal.Interface.Repositories.IPaymentRepository paymentRepository, IMailSender mailSender, ILogger<StudentController> logger) : Controller
    {
        private readonly IReCaptchaService _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        private readonly IStudentService _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
        private readonly IDepartmentService _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        private readonly IMailSender _mailSender = mailSender ?? throw new ArgumentNullException(nameof(mailSender));
        private readonly ILogger<StudentController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly Mitc_Registration_Portal.Interface.Repositories.IRegistrationDraftRepository _draftRepository = draftRepository;
        private readonly Mitc_Registration_Portal.Interface.Repositories.IPaymentRepository _paymentRepository = paymentRepository;
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
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOTP(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                TempData["ErrorMessage"] = "Please enter a valid email address.";
                return RedirectToAction(nameof(VerifyEmail));
            }

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("RegistrationOTP", otp);
            HttpContext.Session.SetString("RegistrationEmail", email);

            var message = $@"
<!DOCTYPE html>
<html lang='en'>
<body style='margin:0; background:#f8fafc; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
<table role='presentation' width='100%' style='background:#f8fafc; padding:40px 15px;'>
  <tr>
    <td align='center'>
      <table role='presentation' width='600' style='background:#ffffff; border-radius:12px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); border-collapse: collapse;'>
        <tr>
          <td style='padding:32px 30px 24px; background:#005bbb; border-radius:12px 12px 0 0; text-align:center;'>
            <img src='cid:mitc_logo.png' style='height:55px; margin-bottom:10px;' alt='MICT Logo' />
            <div style='font-size:22px; font-weight:700; color:#ffffff; letter-spacing:-0.025em;'>Email Verification</div>
          </td>
        </tr>
        <tr>
          <td style='padding:40px 30px;'>
            <p style='font-size:16px; color:#1e293b; margin-top:0;'>Hello Applicant,</p>
            <p style='color:#475569; line-height:1.75; font-size:15px;'>
              Welcome to the MITC Registration Portal! To complete your email verification, please use the following 6-digit verification code:
            </p>
            <div style='margin:28px 0; padding:20px; background:#eff6ff; border:1px solid #bfdbfe; border-radius:10px; text-align:center;'>
              <div style='font-size:36px; font-weight:800; color:#005bbb; letter-spacing:8px; font-family: monospace;'>{otp}</div>
            </div>
            <p style='color:#475569; line-height:1.75; font-size:15px;'>
              This code is valid for 15 minutes. If you did not request this code, please ignore this email.
            </p>
            <p style='font-size:12px; color:#94a3b8; text-align:center; margin-top:40px; margin-bottom:0; border-top: 1px solid #e2e8f0; padding-top: 20px;'>
              This is an automated message from the <strong>MITC Admissions Team</strong>.
            </p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
</body>
</html>";

            var isSent = await _mailSender.Send("buthman111@gmail.com", "MITC Admissions", email, "Applicant", "MITC Email Verification", message);
            
            if (!isSent)
            {
                TempData["ErrorMessage"] = "Failed to send verification code. Please check your email and try again.";
                return RedirectToAction(nameof(VerifyEmail));
            }

            TempData["SuccessMessage"] = "A 6-digit verification code has been sent to your email.";
            return RedirectToAction(nameof(ConfirmOTP));
        }

        [HttpGet]
        public IActionResult ConfirmOTP()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("RegistrationEmail")))
            {
                return RedirectToAction(nameof(VerifyEmail));
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOTP(string otp)
        {
            var storedOtp = HttpContext.Session.GetString("RegistrationOTP");
            if (string.IsNullOrEmpty(storedOtp) || storedOtp != otp.Trim())
            {
                TempData["ErrorMessage"] = "Invalid or expired verification code.";
                return RedirectToAction(nameof(ConfirmOTP));
            }

            HttpContext.Session.SetString("IsEmailVerified", "true");
            return RedirectToAction(nameof(StepOneRegistration));
        }

        [HttpGet]
        public async Task<IActionResult> StepOneRegistration()
        {
            if (HttpContext.Session.GetString("IsEmailVerified") != "true")
            {
                TempData["ErrorMessage"] = "You must verify your email before starting registration.";
                return RedirectToAction(nameof(VerifyEmail));
            }

            var verifiedEmail = HttpContext.Session.GetString("RegistrationEmail");
            CreateStudentRequestModel request = new CreateStudentRequestModel()
            {
                Email = verifiedEmail,
            };
            await PopulateStepOneDepartmentsAsync(request);

            return View(request);
        }
        [HttpPost]
        public async Task<IActionResult> StepOneRegistration(CreateStudentRequestModel model)
        {
            var isHuman = await _captchaService.IsCapchaValid(model.ReCaptchaToken);
            if (!isHuman)
            {
                var captchaMessage = string.IsNullOrWhiteSpace(model.ReCaptchaToken)
                    ? "reCAPTCHA could not run. Refresh the page and try again. If this keeps happening, check that ReCaptchaSiteKey is configured."
                    : "Bot activity detected. Please try again.";
                ModelState.AddModelError("", captchaMessage);
                LogStepOneModelErrors();
                await PopulateStepOneDepartmentsAsync(model);
                return View(model);
            }
            if (!ModelState.IsValid)
            {
                LogStepOneModelErrors();
                await PopulateStepOneDepartmentsAsync(model);
                return View(model);
            }
            var student = await _studentService.StepOneRegistration(model);
            if (!student.Status)
            {
                ModelState.AddModelError("", student.Message ?? "Unable to continue registration.");
                LogStepOneModelErrors();
                await PopulateStepOneDepartmentsAsync(model);
                return View(model);
            }

            return RedirectToAction("StepTwoRegistration");
        }
        [HttpGet]
        [RegistrationStepFilter("Email", "StepTwoRegistration")]
        public IActionResult StepTwoRegistration()
        {
            return View(BuildStepTwoViewModel());
        }
        [HttpPost]
        public async Task<IActionResult> StepTwoRegistration(DocumentViewModel model)
        {
            model = BuildStepTwoViewModel(model);
            ModelState.Clear();
            if (!TryValidateModel(model))
            {
                return View(model);
            }

            await _studentService.StepTwoRegistration(model.waecModel, model.passportModel);
            return RedirectToAction("Index", "Payment");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllStudent()
        {
            var getAllStudent = await _studentService.StudentAsync();

            try
            {
                var drafts = await _draftRepository.GetAll<Mitc_Registration_Portal.Models.Entities.RegistrationDraft>();
                var payments = await _paymentRepository.GetAll<Mitc_Registration_Portal.Models.Entities.Payment>();

                ViewBag.TotalDrafts = drafts.Count;
                ViewBag.TotalRevenue = payments.Where(p => p.PaymentStatus == "Successful" || p.PaymentStatus == "success" || p.PaymentStatus == "Success").Sum(p => p.Amount);
            }
            catch (Exception)
            {
                ViewBag.TotalDrafts = 0;
                ViewBag.TotalRevenue = 0m;
            }

            return View(getAllStudent);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AllStudents()
        {
            return RedirectToAction(nameof(GetAllStudent));
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
            await PopulateDepartmentOptionsAsync(student.Data);
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
                    student.Data.DepartmentId = model.DepartmentId;
                    await PopulateDepartmentOptionsAsync(student.Data);
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
    [Authorize(Roles = "Admin")]
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

        private static DocumentViewModel BuildStepTwoViewModel(DocumentViewModel? model = null)
        {
            var viewModel = model ?? new DocumentViewModel
            {
                waecModel = new CreateNewWaecModel(),
                passportModel = new CreatePassportRequestModel()
            };

            viewModel.waecModel ??= new CreateNewWaecModel();
            viewModel.passportModel ??= new CreatePassportRequestModel();

            viewModel.waecModel.ExaminationNumber = viewModel.waecModel.ExaminationNumber?.Trim() ?? string.Empty;
            viewModel.waecModel.ExaminationYear = viewModel.waecModel.ExaminationYear?.Trim() ?? string.Empty;
            viewModel.waecModel.SecondExaminationNumber = viewModel.waecModel.SecondExaminationNumber?.Trim();
            viewModel.waecModel.SecondExaminationYear = viewModel.waecModel.SecondExaminationYear?.Trim();

            viewModel.waecModel.SubjectResults ??= [];
            viewModel.waecModel.SubjectResults = viewModel.waecModel.SubjectResults
                .Select(result => new WaecSubjectResultEntryModel
                {
                    Subject = result.Subject?.Trim(),
                    Grade = result.Grade?.Trim()?.ToUpperInvariant(),
                    SittingNumber = result.SittingNumber <= 0 ? 1 : result.SittingNumber
                })
                .ToList();

            if (viewModel.waecModel.SittingType == 0)
            {
                viewModel.waecModel.SittingType = ResultSittingType.OneSitting;
            }

            if (viewModel.waecModel.SittingType == ResultSittingType.OneSitting)
            {
                viewModel.waecModel.SecondExaminationNumber = null;
                viewModel.waecModel.SecondExaminationYear = null;
                foreach (var result in viewModel.waecModel.SubjectResults)
                {
                    result.SittingNumber = 1;
                }
            }

            while (viewModel.waecModel.SubjectResults.Count < 9)
            {
                viewModel.waecModel.SubjectResults.Add(new WaecSubjectResultEntryModel());
            }

            return viewModel;
        }

        private async Task PopulateStepOneDepartmentsAsync(CreateStudentRequestModel model)
        {
            var departments = await _departmentService.GetAllDepartmentsAsync();
            model.DepartmentsSelectListItems = departments.Data?.Select(dpt => new SelectListItem
            {
                Text = dpt.DepartmentName,
                Value = dpt.Id.ToString(),
                Selected = dpt.Id == model.DepartmentId
            });
        }

        private void LogStepOneModelErrors()
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => new
                {
                    Field = string.IsNullOrWhiteSpace(entry.Key) ? "Form" : entry.Key,
                    Error = error.ErrorMessage
                }))
                .ToList();

            foreach (var error in errors)
            {
                _logger.LogWarning("Step 1 registration validation failed. Field: {Field}. Error: {Error}", error.Field, error.Error);
            }
        }

        private async Task PopulateDepartmentOptionsAsync(StudentDtos student)
        {
            var departments = await _departmentService.GetAllDepartmentsAsync();
            var departmentItems = departments.Data?.Select(department => new SelectListItem
            {
                Text = department.DepartmentName,
                Value = department.Id.ToString(),
                Selected = department.Id == student.DepartmentId
            }).ToList() ?? [];

            student.DepartmentsSelectListItems = departmentItems;

            if (string.IsNullOrWhiteSpace(student.DepartmentName))
            {
                student.DepartmentName = departmentItems
                    .Where(item => item.Selected)
                    .Select(item => item.Text)
                    .FirstOrDefault();
            }
        }

    }
}
