using FluentValidation;

namespace Mitc_Registration_Portal.Models.DTOs.Waecs.Validation
{
    public class CreateWaecValidation : AbstractValidator<CreateNewWaecModel>
    {
        public CreateWaecValidation()
        {
            RuleFor(x => x.WaecCertificate).NotNull().WithMessage("Certiificate is required")
                .Must(BeValidSize).WithMessage("Picture must be less than 2mb")
                .Must(BeValidType).WithMessage("Only JPG,JPEG and PNG are allowed");
            RuleFor(x => x.ExaminationYear).NotEmpty().WithMessage("Examination Year is required")
                .Matches("^[0-9]{4}$").WithMessage("Examination Year must be a valid year");
            RuleFor(x => x.ExaminationNumber).NotEmpty().WithMessage("Examination Number is required");
        }
        private bool BeValidSize(IFormFile file)
        {
            if (file == null) return false;
            return file.Length <= 2 * 1024 * 1024;
        }
        private bool BeValidType(IFormFile file)
        {
            if (file == null) return false;
            var allowed = new[] { "image/jpeg", "image/jpg", "image/png" };
            return allowed.Contains(file.ContentType);
        }
    }
}
