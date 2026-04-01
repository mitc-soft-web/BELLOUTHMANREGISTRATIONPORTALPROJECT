using FluentValidation;
using Mitc_Registration_Portal.Models.DTOs.Passports.Validation;
using Mitc_Registration_Portal.Models.DTOs.Waecs.Validation;

namespace Mitc_Registration_Portal.Models.DTOs.Document.Validation
{
    // Prevents bypass where the nested models are null (child validators won't run).
    public class DocumentViewModelValidation : AbstractValidator<DocumentViewModel>
    {
        public DocumentViewModelValidation()
        {
            RuleFor(x => x.waecModel)
                .NotNull().WithMessage("WAEC details are required.")
                .SetValidator(new CreateWaecValidation());

            RuleFor(x => x.passportModel)
                .NotNull().WithMessage("Passport details are required.")
                .SetValidator(new PassportValidation());
        }
    }
}

