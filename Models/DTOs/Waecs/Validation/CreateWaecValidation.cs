using FluentValidation;
using Mitc_Registration_Portal.Models.DTOs.Waecs;
using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Models.DTOs.Waecs.Validation
{
    public class CreateWaecValidation : AbstractValidator<CreateNewWaecModel>
    {
        public CreateWaecValidation()
        {
            RuleFor(x => x.ExaminationYear)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("First sitting examination year is required.")
                .Matches("^[0-9]{4}$").WithMessage("Examination year must be a valid year.")
                .Must(BeNotInFuture).WithMessage("Examination year cannot be greater than the current year.");

            RuleFor(x => x.ExaminationNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("First sitting examination number is required.")
                .MaximumLength(30).WithMessage("Examination number is too long.");

            RuleFor(x => x.ExaminationBody)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("First sitting examination body is required.")
                .Must(body => body != null && new[] { "WAEC", "NECO", "NABTEB", "GCE", "JUPEB", "IJMB" }.Contains(body.ToUpperInvariant()))
                .WithMessage("Select a valid examination body.");

            RuleFor(x => x.SittingType)
                .IsInEnum().WithMessage("Select whether the result is one sitting or two sittings.");

            When(x => x.SittingType == ResultSittingType.TwoSittings, () =>
            {
                RuleFor(x => x.SecondExaminationYear)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("Second sitting examination year is required.")
                    .Matches("^[0-9]{4}$").WithMessage("Second sitting examination year must be a valid year.")
                    .Must(year => year != null && BeNotInFuture(year)).WithMessage("Second sitting examination year cannot be greater than the current year.");

                RuleFor(x => x.SecondExaminationNumber)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("Second sitting examination number is required.")
                    .MaximumLength(30).WithMessage("Second sitting examination number is too long.");

                RuleFor(x => x.SecondExaminationBody)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("Second sitting examination body is required.")
                    .Must(body => body != null && new[] { "WAEC", "NECO", "NABTEB", "GCE", "JUPEB", "IJMB" }.Contains(body.ToUpperInvariant()))
                    .WithMessage("Select a valid examination body for the second sitting.");
            });

            RuleFor(x => x.SubjectResults)
                .NotNull().WithMessage("Enter the WAEC or NECO subjects and grades.")
                .Must(HaveAtLeastOneCompletedResult).WithMessage("Enter at least one subject result.");

            RuleForEach(x => x.SubjectResults)
                .SetValidator(new WaecSubjectResultEntryValidation());

            RuleFor(x => x)
                .Must(HaveValidSittingAssignments)
                .WithMessage("Each entered subject result must match the selected sitting option.");
        }

        private static bool BeNotInFuture(string year)
        {
            if (!int.TryParse(year, out var y)) return false;
            var currentYear = DateTime.UtcNow.Year;
            return y >= 1950 && y <= currentYear;
        }

        private static bool HaveAtLeastOneCompletedResult(List<WaecSubjectResultEntryModel>? results)
        {
            return results != null && results.Any(IsCompletedResult);
        }

        private static bool HaveValidSittingAssignments(CreateNewWaecModel model)
        {
            var completedResults = (model.SubjectResults ?? [])
                .Where(IsCompletedResult)
                .ToList();

            if (!completedResults.Any())
            {
                return true;
            }

            return model.SittingType switch
            {
                ResultSittingType.OneSitting => completedResults.All(result => result.SittingNumber == 1),
                ResultSittingType.TwoSittings => completedResults.All(result => result.SittingNumber is 1 or 2),
                _ => false,
            };
        }

        private static bool IsCompletedResult(WaecSubjectResultEntryModel result)
        {
            return !string.IsNullOrWhiteSpace(result.Subject) || !string.IsNullOrWhiteSpace(result.Grade);
        }
    }
}
