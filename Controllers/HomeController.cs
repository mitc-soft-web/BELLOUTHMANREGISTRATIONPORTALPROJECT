using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mitc_Registration_Portal.Models;

namespace Mitc_Registration_Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Home/Error/{statusCode?}")]
        public IActionResult Error(int? statusCode = null)
        {
            var code = statusCode ?? HttpContext.Response.StatusCode;
            if (code == 200)
            {
                code = 500;
            }

            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = code
            };

            switch (code)
            {
                case 400:
                    model.Title = "Bad Request";
                    model.Message = "The request could not be processed due to invalid parameters or malformed syntax.";
                    break;
                case 401:
                    model.Title = "Authentication Required";
                    model.Message = "You must be logged in to view this page. Please sign in to your MITC account.";
                    break;
                case 403:
                    model.Title = "Access Restricted";
                    model.Message = "You do not have the required permissions to access this administrative resource.";
                    break;
                case 404:
                    model.Title = "Page Not Found";
                    model.Message = "The page, course, or department record you were looking for doesn't exist or may have been relocated.";
                    break;
                case 500:
                    model.Title = "Server Encountered an Error";
                    model.Message = "We experienced an unexpected problem loading this resource. The system log has captured this incident.";
                    break;
                default:
                    model.Title = "Unexpected Error";
                    model.Message = "Something unexpected occurred while processing your request. Please try again or return to the portal.";
                    break;
            }

            return View(model);
        }
    }
}
