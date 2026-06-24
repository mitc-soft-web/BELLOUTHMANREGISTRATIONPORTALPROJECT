using FluentValidation;

namespace Mitc_Registration_Portal.Models.DTOs.Passports.Validation
{
    public class PassportValidation : AbstractValidator<CreatePassportRequestModel>
    {
        public PassportValidation()
        {
            RuleFor(x => x.PassportPhoto)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Image is required")
                .Must(f => f != null && f.Length > 0).WithMessage("Image file is empty")
                .Must(BeValidSize).WithMessage("Picture must be less than 5mb")
                .Must(BeValidType).WithMessage("Only JPG, JPEG and PNG are allowed")
                .Must(BeValidExtension).WithMessage("Only .jpg, .jpeg and .png extensions are allowed")
                .Must(BeValidSignature).WithMessage("Invalid image file");
        }

        private bool BeValidSize(IFormFile file)
        {
            return file != null && file.Length <= 5 * 1024 * 1024;
        }

        private bool BeValidType(IFormFile file)
        {
            if (file == null) return false;
            var allowed = new[] { "image/jpeg", "image/jpg", "image/png" };
            return allowed.Contains(file.ContentType);
        }

        private bool BeValidExtension(IFormFile file)
        {
            if (file == null) return false;
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png";
        }

        private bool BeValidSignature(IFormFile file)
        {
            if (file == null) return false;
            try
            {
                using var stream = file.OpenReadStream();
                Span<byte> header = stackalloc byte[8];
                var read = stream.Read(header);
                if (read < 3) return false;

                // JPEG: FF D8 FF
                if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;

                // PNG: 89 50 4E 47 0D 0A 1A 0A
                if (read >= 8 &&
                    header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                    header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A) return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

