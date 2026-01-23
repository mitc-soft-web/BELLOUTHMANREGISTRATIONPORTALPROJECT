using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mitc_Registration_Portal.Filters
{
    public class RegistrationStepFilter : ActionFilterAttribute
    {
        private readonly string _requiredSessionKey;
        private readonly string _redirectTo;
        public RegistrationStepFilter(string requiredSessionKey,string redirectTo)
        {
            _requiredSessionKey = requiredSessionKey;
            _redirectTo = redirectTo;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if(string.IsNullOrEmpty(session.GetString(_requiredSessionKey)))
            {
                context.Result = new RedirectToActionResult(_redirectTo, "Student", null);
            }
            base.OnActionExecuting(context);
        }
    }
}
