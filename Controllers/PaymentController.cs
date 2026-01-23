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
        public async Task<IActionResult> CallBack()
        {
            var isSuccess = await _studentService.VerifyPaymentAsync();
            if(isSuccess.Status)
            {
                return RedirectToAction("Index", "Student");
            }
            return View("PaymentFailed");
        }

    }
}
