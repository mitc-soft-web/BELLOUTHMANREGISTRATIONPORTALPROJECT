using FluentValidation;

namespace Mitc_Registration_Portal.Models.DTOs.Users.Validation
{
    public class LoginValidation : AbstractValidator<LoginRequestModel>
    {
        public LoginValidation()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        }
    }
}
