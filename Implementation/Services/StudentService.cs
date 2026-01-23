using System;
using System.Threading.Tasks;
using Findx.Data;
using Findx.Extensions;
using Microsoft.AspNetCore.Identity;
using Mitc_Registration_Portal.Contracts.Services;
using Mitc_Registration_Portal.Implementation.Repositories;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Passports;
using Mitc_Registration_Portal.Models.DTOs.Payments;
using Mitc_Registration_Portal.Models.DTOs.Students;
using Mitc_Registration_Portal.Models.DTOs.Waecs;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Models.Enums;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class StudentService(IStudentRepository studentRepository, IHttpContextAccessor httpContextAccessor, IUserRepository userRepository,
       IIdentityService identityService, ILogger<StudentService> logger,IWebHostEnvironment env,IPaymentService paymentService,
        Interface.Repositories.IUnitOfWork unitOfWork,UserManager<User> userManager,IPaymentRepository paymentRepository) : IStudentService
    {
        private readonly UserManager<User> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        private readonly Interface.Repositories.IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IStudentRepository _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        private readonly IPaymentService _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(_userRepository));
        private readonly IIdentityService _identityService = identityService ?? throw new ArgumentNullException( nameof(identityService));
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
        private readonly ILogger<StudentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        private readonly IPaymentRepository _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        

        private ISession Session => _httpContextAccessor.HttpContext.Session;

        public async Task<BaseResponse<bool>> CreateAsync()
        {
            var email = Session.GetString("Email");
            var genders = Session.GetString("Gender");
            string password = Session.GetString("Password");
            string confirmPassword = Session.GetString("ConfirmPassword");
            string firstName = Session.GetString("FirstName");
            string middleName = Session.GetString("MiddleName");
            string lastName = Session.GetString("LastName");
            string phoneNumber = Session.GetString("PhoneNumber");
            string address = Session.GetString("Address");
            string city = Session.GetString("City");
            string state = Session.GetString("State");
            string nin = Session.GetString("Nin");
            string dateOfBirth = Session.GetString("DateOfBirth");
            //STep Two
            string examNo = Session.GetString("ExamNo");
            string year = Session.GetString("Year");
            string certificates = Session.GetString("certificates");
            string passport = Session.GetString("passports");
            string reference = Session.GetString("Reg_PayRef");
            DateTime dateOfBit = Convert.ToDateTime(dateOfBirth);
            //Gender gender = (Gender)Enum.Parse(typeof(Gender), genders);
            if (!Enum.TryParse<Gender>(genders, out var gender))
            {
                gender = Gender.Other;
            }
                Guid DeptId = Guid.Parse(Session.GetString("DeptId"));

            var studentExists = await _userRepository.Any(x => x.Email == email);
            if (studentExists)
            {
                _logger.LogError("Student with email already exist");
                return new BaseResponse<bool>
                {
                    Message = "Student with email already exist",
                    Status = false
                };
            }
            if (password != confirmPassword)
                return new BaseResponse<bool>
                {
                    Message = "Password doesn't match!",
                    Status = false,
                };
            (var passwordResult, var message) = ValidatePassword(password);
            if (!passwordResult) return new BaseResponse<bool> { Message = message, Status = false };
            // Revert to the previously working approach: minimal User object, use identity service to set PasswordHash
            var studentUser = new User
            {
                Email = email,
                DateCreated = DateTime.UtcNow,
            };
            // Use your identity service to create the hash (as before)
            studentUser.PasswordHash = _identityService.GetPasswordHash(password);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Create user (previous working call)
                var createResult = await _userManager.CreateAsync(studentUser);
                if (createResult == null || !createResult.Succeeded)
                {
                    var errors = createResult == null
                        ? "Unknown error creating user"
                        : string.Join(" | ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("User creation unsuccessful: {Errors}", errors);

                    await transaction.RollbackAsync();
                    return new BaseResponse<bool>
                    {
                        Message = "User creation unsuccessful: " + errors,
                        Status = false,
                    };
                }

                // Add to Customer role
                var addRoleResult = await _userManager.AddToRoleAsync(studentUser, "Student");
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                    _logger.LogError("Unable to add user to role: {Errors}", errors);
                    await transaction.RollbackAsync();
                    return new BaseResponse<bool>
                    {
                        Message = "Unable to add user to role: " + errors,
                        Status = false
                    };
                }
            if (string.IsNullOrWhiteSpace(passport))
                {
                    _logger.LogWarning("Passport path missing in session for email {Email}", email);
                    return new BaseResponse<bool>
                    {
                        Status = false,
                        Message = "Passport not uploaded. Complete step two and try again."
                    };
                }
                if (string.IsNullOrWhiteSpace(certificates))
                {
                    _logger.LogWarning("Certificates path missing in session for email {Email}", email);
                    return new BaseResponse<bool>
                    {
                        Status = false,
                        Message = "WAEC certificate not uploaded. Complete step two and try again."
                    };
                }
                // --- then continue building student and child entities as before ---
                var student = new Student
                {
                    UserId = studentUser.Id,
                    DepartmentId = DeptId,
                    FirstName = firstName,
                    MiddleName = middleName,
                    RegistrationNumber = RegistrationNumber(),
                    Status = Status.Pending,
                    State = state,
                    LastName = lastName,
                    Address = address,
                    City = city,
                    DateCreated = DateTime.UtcNow,
                    DateOfBirth = dateOfBit,
                    Gender = gender,
                    Email = email,
                    IsAdmitted = false,
                    NIN = nin,
                    PhoneNumber = phoneNumber,
                    Passport = new Passport
                    {
                      PassportUrl = passport,
                      DateCreated = DateTime.UtcNow,
                    },
                    Waec = new Waec
                    {
                        ExaminationNumber = examNo,
                        ExaminationYear = year,
                        DateCreated = DateTime.UtcNow,
                    }
                                   
                };
                var existingPayment = await _paymentRepository.GetByReference(reference);
                if (existingPayment != null)
                {
                    // link the tracked Payment entity with the new Student via navigation
                    existingPayment.PaymentStatus = "Completed";

                    // establish both sides of the relationship so EF can persist FKs correctly
                    student.Payment = existingPayment;
                    existingPayment.Student = student;

                    // DO NOT call _paymentRepository.Update(existingPayment) if the entity is tracked.
                    // EF will detect the modified PaymentStatus and will insert the new Student and update the Payment FK in one SaveChanges.
                }
                else
                {
                    _logger.LogWarning("Payment not found for reference {Reference}", reference);
                }
                var createStudent = await _studentRepository.Add<Student>(student);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);

                if (createStudent == null)
                {
                    _logger.LogError("Student Couldn't be Added");
                    await transaction.RollbackAsync();
                    return new BaseResponse<bool>
                    {
                        Message = "Student Couldn't be Added",
                        Status = false
                    };
                }
                await transaction.CommitAsync();

                _logger.LogInformation("Student Added Successfully");
                return new BaseResponse<bool>
                {
                    Message = "Student Added Successfully",
                    Status = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer, rolling back.....");
                try { await transaction.RollbackAsync(); } catch (Exception rbEx) { _logger.LogError(rbEx, "Rollback failed"); }
                return new BaseResponse<bool>
                {
                    Message = "An error occurred while creating Student",
                    Status = false
                };
            }
        }

        public async Task<BaseResponse<PayStackInitDto>> InitiateRegistration()
        {
            string email = Session.GetString("Email");
           
            var response = await _paymentService.IniatilizeRegistrationPayment(email);
            if(response.Status)
            {
                Session.SetString("Reg_PayRef", response.Data.Reference);
            }
            return response;
        }
        public async Task<BaseResponse<bool>> VerifyPaymentAsync()
        {
            string referenceVerification = Session.GetString("Reg_PayRef");
            if(referenceVerification == null)
            {
                return new BaseResponse<bool> { Status = false,Message="Reference not matching"};
            }
            var isPaymentSuccessful = await _paymentService.VerifyPaymentAsync(referenceVerification);
            if(!isPaymentSuccessful)
            {
                return new BaseResponse<bool>
                {
                    Message = "Verification failed",
                    Status = false,

                };
            }

            return await CreateAsync();
        }
        public void StepOneRegistration(CreateStudentRequestModel model)
        {
            if (model == null)
            {
                _logger.LogError("User Must Input all value");
            }
            Session.SetString("FirstName", model.FirstName);
            Session.SetString("LastName", model.LastName);
            string? gender = model.Gender.ToString();
            Session.SetString("Gender", gender);
            Session.SetString("PhoneNumber", model.PhoneNumber);
            Session.SetString("City", model.City);
            Session.SetString("State", model.State);
            Session.SetString("Address", model.Address);
            Session.SetString("Nin", model.NIN);
            Session.SetString("Email", model.Email);
            Session.SetString("MiddleName", model.MiddleName);
            Session.SetString("Password", model.Password);
            Session.SetString("ConfirmPassword", model.ConfirmPassword);
            string? dateofbit = model.DateOfBirth.ToString();
            Session.SetString("DateOfBirth", dateofbit);
            string? id = model.DepartmentId.ToString();
            Session.SetString("DeptId", id);

        }

        public async Task StepTwoRegistration(CreateNewWaecModel waecModel, CreatePassportRequestModel passportModel)
        {
            Session.SetString("ExamNo", waecModel.ExaminationNumber);
            Session.SetString("Year", waecModel.ExaminationYear);
            if (waecModel.WaecCertificate != null)
            {
                var waecPath = await SaveFileToDisk(waecModel.WaecCertificate, "certificates");
                Session.SetString("certificates", waecPath);
            }
            if (passportModel.PassportPhoto != null)
            {
                var passportPath = await SaveFileToDisk(passportModel.PassportPhoto, "passports");
                Session.SetString("passports", passportPath);
            }

        }
        public async Task<BaseResponse<IEnumerable<StudentDtos>>> StudentAsync()
        {
            var getDepartments = await _studentRepository.GetAll<Student>();
            if(getDepartments == null)
            {
                _logger.LogError("Student not found");
                return new BaseResponse<IEnumerable<StudentDtos>>
                {
                    Status = false,
                    Message = "Student not found",
                };
            }
            return new BaseResponse<IEnumerable<StudentDtos>>
            {
                Message = "Student gotten successfully",
                Status = true,
                Data = getDepartments.Select(s => new StudentDtos
                {
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.Email,
                    DateCreated = s.DateCreated,
                    State = s.State,
                    DateOfBirth = s.DateOfBirth,
                    Gender = s.Gender
                }).ToList()
            };
        }

        private async Task<string> SaveFileToDisk(IFormFile file, string subFolder)
        {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string uploadDir = Path.Combine(_env.WebRootPath, "uploads", subFolder);

            if(!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
            string fullPath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(fullPath,FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/{subFolder}/{fileName}";
        }
        private string RegistrationNumber()
        {
            return $"MITC/" + Guid.NewGuid().ToString().Substring(0, 8).Replace("-", "").Trim();
        }
        private static (bool, string?) ValidatePassword(string password)
        {
            // Minimum length of password
            int minLength = 8;

            // Maximum length of password
            int maxLength = 50;

            // Check for null or empty password
            if (string.IsNullOrEmpty(password))
            {
                return (false, "Password cannot be null or empty.");
            }

            // Check length of password
            if (password.Length < minLength || password.Length > maxLength)
            {
                return (false, $"Password must be between {minLength} and {maxLength} characters long.");
            }

            // Check for at least one uppercase letter, one lowercase letter, and one digit
            bool hasUppercase = false;
            bool hasLowercase = false;
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c))
                {
                    hasUppercase = true;
                }
                else if (char.IsLower(c))
                {
                    hasLowercase = true;
                }
                else if (char.IsDigit(c))
                {
                    hasDigit = true;
                }
            }

            if (!hasUppercase || !hasLowercase || !hasDigit)
            {
                return (false, "Password must contain at least one uppercase letter, one lowercase letter, and one digit.");
            }

            // Check for any characters
            string invalidCharacters = @" !""#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
            if (password.IndexOfAny(invalidCharacters.ToCharArray()) == -1)
            {
                return (false, "Password must contain one or more characters.");
            }

            // Password is valid
            return (true, null);
        }

       
    }
}
