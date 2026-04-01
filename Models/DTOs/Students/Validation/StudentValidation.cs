using FluentValidation;

namespace Mitc_Registration_Portal.Models.DTOs.Students.Validation
{
    public class StudentValidation : AbstractValidator<CreateStudentRequestModel>
    {
        public StudentValidation()
        {
            RuleFor(x => x.FirstName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Firstname is required")
                .Length(3, 50)
                .Matches("^[A-Za-z][A-Za-z\\s'\\-]*$").WithMessage("Firstname can only contain letters, spaces, apostrophes and hyphens");

            // Middle name is optional in the UI. Validate only when provided.
            RuleFor(x => x.MiddleName)
                .Cascade(CascadeMode.Stop)
                .MaximumLength(50).WithMessage("MiddleName is too long")
                .Matches("^[A-Za-z][A-Za-z\\s'\\-]*$").WithMessage("MiddleName can only contain letters, spaces, apostrophes and hyphens")
                .When(x => !string.IsNullOrWhiteSpace(x.MiddleName));

            RuleFor(x => x.LastName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Lastname is required")
                .Length(3, 50)
                .Matches("^[A-Za-z][A-Za-z\\s'\\-]*$").WithMessage("Lastname can only contain letters, spaces, apostrophes and hyphens");

            RuleFor(x => x.City)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("City Name is required")
                .Length(2, 50)
                .Matches("^[A-Za-z][A-Za-z\\s'\\-]*$").WithMessage("City can only contain letters, spaces, apostrophes and hyphens");

            RuleFor(x => x.State)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("State name is required")
                .Length(2, 50)
                .Matches("^[A-Za-z][A-Za-z\\s'\\-]*$").WithMessage("State can only contain letters, spaces, apostrophes and hyphens");

            RuleFor(x => x.NIN)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("NIN is required")
                .Length(11).WithMessage("NIN must be 11 digits")
                .Matches("^[0-9]+$").WithMessage("NIN must contain only numbers");

            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email must be in a correct format")
                .MaximumLength(254).WithMessage("Email is too long");

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .MaximumLength(50).WithMessage("Password must be at most 50 characters long")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit")
                .Matches("[ !\"#$%&'()*+\\,\\-\\./:;<=>\\?@\\[\\\\\\]\\^_`\\{\\|\\}~]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Confirm password is required")
                .Equal(x => x.Password).WithMessage("Password must match");

            RuleFor(x => x.Address)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Address is required")
                .MinimumLength(5).WithMessage("Address is too short")
                .MaximumLength(200).WithMessage("Address is too long");

            RuleFor(x => x.DateOfBirth)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Date of birth is required")
                .Must(BeAtLeast14).WithMessage("You must be at least 14 years old")
                .GreaterThan(new DateTime(1900, 1, 1)).WithMessage("Date Of Birth is too far in the past");

            RuleFor(x => x.Gender)
                .NotEmpty().WithMessage("Gender is required");

            RuleFor(x => x.PhoneNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches("^\\+234[1-9][0-9]{9}$").WithMessage("Phone number must be in +234XXXXXXXXXX format");

            RuleFor(x => x.DepartmentId)
                .NotEmpty().WithMessage("Department is required");
        }

        private static bool BeAtLeast14(DateTime dob)
        {
            var today = DateTime.Today;
            if (dob.Date > today) return false;

            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;

            return age >= 14;
        }
    }
}

