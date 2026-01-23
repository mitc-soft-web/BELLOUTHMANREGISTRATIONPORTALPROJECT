using FluentValidation;

namespace Mitc_Registration_Portal.Models.DTOs.Departments.Validation
{
    public class DepartmentValidation : AbstractValidator<CreateDepartmentRequestModel>
    {
        public DepartmentValidation() 
        {
            RuleFor(x => x.DepartmentName).Length(3,50).NotEmpty().WithMessage("DepartmentName is required");
            RuleFor(x => x.DepartmentalCode).Length(3).WithMessage("Code must be three letters").NotEmpty().WithMessage("DepartmentalCode is required");


        }
    }
}
