using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Interface.Services;

namespace Mitc_Registration_Portal.Controllers
{
    public class PaymentController(
        IStudentService studentService,
        IOptions<PaystackConfiguration> paystackOptions) : Controller
    {
        private readonly IStudentService _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
        private readonly PaystackConfiguration _paystack = paystackOptions.Value;

        public IActionResult Index()
        {
            ViewBag.RegistrationFeeNaira = _paystack.RegistrationFeeNaira;
            ViewBag.RegistrationFeeDisplay = FormatNaira(_paystack.RegistrationFeeNaira);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StepThreeRegistration()
        {
            ViewBag.RegistrationFeeNaira = _paystack.RegistrationFeeNaira;
            ViewBag.RegistrationFeeDisplay = FormatNaira(_paystack.RegistrationFeeNaira);

            var response = await _studentService.InitiateRegistration();
            if (response.Status && response.Data != null)
            {
                var target = response.Data.AuthorizationUrl;
                if (Uri.TryCreate(target, UriKind.Absolute, out var absolute) &&
                    (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
                {
                    return Redirect(target);
                }

                if (!string.IsNullOrWhiteSpace(target) && target.StartsWith('/'))
                {
                    return Redirect(target);
                }

                ViewBag.PaymentError = "Payment could not be started because the gateway returned an invalid redirect.";
                return View("Index");
            }

            ViewBag.PaymentError = string.IsNullOrWhiteSpace(response.Message)
                ? "Payment could not be started. Please check your internet connection and try again."
                : response.Message;

            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> CallBack([FromQuery] string? reference)
        {
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

        private static string FormatNaira(decimal amount) =>
            amount.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("en-NG"));
    }
}
