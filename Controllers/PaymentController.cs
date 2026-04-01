using Microsoft.AspNetCore.Mvc;
using Mitc_Registration_Portal.Interface.Services;

namespace Mitc_Registration_Portal.Controllers
{
    public class PaymentController(IStudentService studentService) : Controller
    {
        private readonly IStudentService _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> StepThreeRegistration()
        {
            var response = await _studentService.InitiateRegistration();
            if (response.Status && response.Data != null)
            {
                return Redirect(response.Data.AuthorizationUrl);
            }

            return View("Error");
        }

        [HttpGet]
        public async Task<IActionResult> CallBack([FromQuery] string? reference)
        {
            // Paystack redirects back with a `reference` query param.
            if (!string.IsNullOrWhiteSpace(reference))
            {
                HttpContext.Session.SetString("Reg_PayRef", reference);
            }

            var result = await _studentService.VerifyPaymentAsync(reference);
            if (result.Status)
            {
                return RedirectToAction("Login", "User");
            }

            return View("PaymentFailed", result);
        }
    }
}

