using FluentValidation;

namespace Mitc_Registration_Portal.Models.DTOs.Departments.Validation
{
    public class DepartmentValidation : AbstractValidator<CreateDepartmentRequestModel>
    {
        public DepartmentValidation()
        {
            RuleFor(x => x.DepartmentName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("DepartmentName is required")
                .Length(3, 50);

            RuleFor(x => x.DepartmentalCode)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("DepartmentalCode is required")
                .Length(3).WithMessage("Code must be three letters")
                .Matches("^[A-Za-z]{3}$").WithMessage("Code must be three letters");
        }
    }
}

