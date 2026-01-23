using FluentValidation;
using FluentValidation.Validators;

namespace Mitc_Registration_Portal.Models.DTOs.Students.Validation
{
    public class StudentValidation : AbstractValidator<CreateStudentRequestModel>
    {
        public StudentValidation() 
        {
            RuleFor(x => x.FirstName).Length(3, 50).NotEmpty().WithMessage("Firstname is required");
            RuleFor(x => x.MiddleName).Length(3, 50).NotEmpty().WithMessage("MiddleName is required");
            RuleFor(x => x.LastName).Length(3, 50).NotEmpty().WithMessage("Lastname is required");
            RuleFor(x => x.City).Length(3, 50).NotEmpty().WithMessage("City Name is required");
            RuleFor(x => x.State).Length(3, 50).NotEmpty().WithMessage("State name is required");
            RuleFor(x => x.NIN).Length(11).WithMessage("NIN must be 11 figures").NotEmpty().WithMessage("NIN is required")
                .Matches("^[0-9]+$").WithMessage("NIN must contain only numbers");
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required").EmailAddress().WithMessage("Email Must Be In A Correct Format");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
            RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage("Password Must Match").Equal(x => x.Password).WithMessage("Password Must Match");
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required");
            RuleFor(x => x.DateOfBirth).NotEmpty().WithMessage("Date of birth is required").LessThan(new DateTime(2016, 1, 1))
                .WithMessage("Date Of Birth Cannot be today or a future day").GreaterThan(new DateTime(1900, 1, 1));
            RuleFor(x => x.Gender).NotEmpty().WithMessage("Gender is required");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required")
                .Matches("^[0-9]+$").WithMessage("Phone Number must contain only numbers")
                .Length(11).WithMessage("Phone number must be 11 Digits ");
        }

    }
}
