using FluentValidation;

namespace Mitc_Registration_Portal.Models.DTOs.Passports.Validation
{
    public class PassportValidation : AbstractValidator<CreatePassportRequestModel>
    {
        public PassportValidation()
        {
            RuleFor(x => x.PassportPhoto).NotNull().WithMessage("Image is required")
                .Must(BeValidSize).WithMessage("Picture must be less than 2mb")
                .Must(BeValidType).WithMessage("Only JPG,JPEG and PNG are allowed");
        }
        private bool BeValidSize(IFormFile file)
        {
            if(file == null) return false;
            return file.Length <= 2 * 1024 * 1024;
        }
        private bool BeValidType(IFormFile file)
        {
            if(file == null) return false;
            var allowed = new[] { "image/jpeg", "image/jpg", "image/png" };
            return allowed.Contains(file.ContentType);
        }

    }
}
