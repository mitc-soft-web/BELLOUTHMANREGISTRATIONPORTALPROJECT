using FluentValidation;

namespace Mitc_Registration_Portal.Models.DTOs.Students.Validation
{
    public class UpdateStudentAdmissionRequestModelValidation : AbstractValidator<UpdateStudentAdmissionRequestModel>
    {
        public UpdateStudentAdmissionRequestModelValidation()
        {
            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Select the department or course to assign to the student.");
        }
    }
}
