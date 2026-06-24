using FluentValidation;
using Mitc_Registration_Portal.Models.DTOs.Waecs;

namespace Mitc_Registration_Portal.Models.DTOs.Waecs.Validation
{
    public class WaecSubjectResultEntryValidation : AbstractValidator<WaecSubjectResultEntryModel>
    {
        private static readonly string[] ValidGrades = ["A1", "B2", "B3", "C4", "C5", "C6", "D7", "E8", "F9", "A", "B", "C", "D", "E", "F"];

        public WaecSubjectResultEntryValidation()
        {
            RuleFor(x => x.Subject)
                .MaximumLength(100).WithMessage("Subject name is too long.");

            When(HasAnyValue, () =>
            {
                RuleFor(x => x.Subject)
                    .NotEmpty().WithMessage("Subject is required for each entered result.");

                RuleFor(x => x.Grade)
                    .NotEmpty().WithMessage("Grade is required for each entered result.")
                    .Must(grade => grade != null && ValidGrades.Contains(grade))
                    .WithMessage("Grade must be a valid O'Level grade (A1-F9) or A'Level grade (A-F).");

                RuleFor(x => x.SittingNumber)
                    .InclusiveBetween(1, 2).WithMessage("Sitting number must be 1 or 2.");
            });
        }

        private static bool HasAnyValue(WaecSubjectResultEntryModel model)
        {
            return !string.IsNullOrWhiteSpace(model.Subject) || !string.IsNullOrWhiteSpace(model.Grade);
        }
    }
}
