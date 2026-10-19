using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Contracts.MailingService;
using Mitc_Registration_Portal.Contracts.Services;
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
    public class StudentService(
        IStudentRepository studentRepository,
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository,
        IIdentityService identityService,
        ILogger<StudentService> logger,
        IWebHostEnvironment env,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        UserManager<User> userManager,
        IPaymentRepository paymentRepository,
        IRegistrationDraftRepository registrationDraftRepository,
        IMailService mailService,
        Microsoft.Extensions.Options.IOptions<PaystackConfiguration> paystackOptions,
        IPayStackService payStackService) : IStudentService
    {
        private readonly UserManager<User> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IStudentRepository _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        private readonly IPaymentService _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        private readonly IIdentityService _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
        private readonly ILogger<StudentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        private readonly IPaymentRepository _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        private readonly IRegistrationDraftRepository _registrationDraftRepository = registrationDraftRepository ?? throw new ArgumentNullException(nameof(registrationDraftRepository));
        private readonly IMailService _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
        private readonly PaystackConfiguration _paystack = paystackOptions?.Value ?? throw new ArgumentNullException(nameof(paystackOptions));
        private readonly IPayStackService _payStackService = payStackService ?? throw new ArgumentNullException(nameof(payStackService));
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _referenceLocks = new();

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public async Task<BaseResponse<bool>> CreateAsync()
        {
            // Legacy session-backed flow. VerifyPaymentAsync prefers drafts and falls back here.
            var email = Session.GetString("Email");
            var genders = Session.GetString("Gender");
            string password = Session.GetString("Password");
            string firstName = Session.GetString("FirstName");
            string middleName = Session.GetString("MiddleName");
            string lastName = Session.GetString("LastName");
            string phoneNumber = Session.GetString("PhoneNumber");
            string address = Session.GetString("Address");
            string city = Session.GetString("City");
            string state = Session.GetString("State");
            string nin = Session.GetString("Nin");
            string dateOfBirth = Session.GetString("DateOfBirth");
            // Step Two
            string examNo = Session.GetString("ExamNo");
            string year = Session.GetString("Year");
            string examBody = Session.GetString("ExamBody");
            string secondExamBody = Session.GetString("SecondExamBody");
            string? waecResultDetailsJson = Session.GetString("WaecResultDetailsJson");
            string passport = Session.GetString("passports");
            string reference = Session.GetString("Reg_PayRef");

            if (string.IsNullOrWhiteSpace(email))
            {
                return new BaseResponse<bool> { Status = false, Message = "Session expired. Restart registration." };
            }

            DateTime dateOfBit = Convert.ToDateTime(dateOfBirth);
            if (!Enum.TryParse<Gender>(genders, out var gender))
            {
                gender = Gender.Other;
            }

            Guid deptId = Guid.Parse(Session.GetString("DeptId"));

            var studentUser = new User
            {
                Email = email,
                DateCreated = DateTime.UtcNow,
            };
            studentUser.PasswordHash = _identityService.GetPasswordHash(password);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
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

                var hasWaecResults = GetCompletedWaecResults(waecResultDetailsJson).Any();
                if (!hasWaecResults || string.IsNullOrWhiteSpace(examNo) || string.IsNullOrWhiteSpace(year))
                {
                    _logger.LogWarning("WAEC result details missing in session for email {Email}", email);
                    return new BaseResponse<bool>
                    {
                        Status = false,
                        Message = "WAEC result details are incomplete. Complete step two and try again."
                    };
                }

                var student = new Student
                {
                    UserId = studentUser.Id,
                    DepartmentId = deptId,
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
                        ExaminationBody = examBody,
                        SecondExaminationBody = secondExamBody,
                        ResultDetailsJson = waecResultDetailsJson,
                        DateCreated = DateTime.UtcNow,
                    }
                };

                var existingPayment = await _paymentRepository.GetByReference(reference);
                if (existingPayment != null)
                {
                    student.Payment = existingPayment;
                    existingPayment.Student = student;
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
            var email = Session.GetString("Email");
            if (string.IsNullOrWhiteSpace(email))
            {
                var draftBySession = await TryGetDraftFromSessionAsync();
                email = draftBySession?.Email;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return new BaseResponse<PayStackInitDto>
                {
                    Status = false,
                    Message = "Email not found. Restart registration and try again."
                };
            }

            var draft = await _registrationDraftRepository.GetByEmailAsync(email);

            if (draft != null && !string.IsNullOrWhiteSpace(draft.CurrentPaymentReference))
            {
                var existingPayment = await _paymentRepository.GetByReference(draft.CurrentPaymentReference);
                if (existingPayment != null && existingPayment.PaymentStatus == "Successful")
                {
                    // Check if the student hasn't been created yet due to a previous error
                    var studentExists = await _userRepository.Any(u => u.Email == email);
                    if (!studentExists)
                    {
                        _logger.LogInformation("Found successful payment {Reference} for email {Email} without an account. Retrying account creation.", draft.CurrentPaymentReference, email);
                        var retryResult = await VerifyPaymentAsync(draft.CurrentPaymentReference);
                        if (retryResult.Status)
                        {
                            return new BaseResponse<PayStackInitDto>
                            {
                                Status = true,
                                Message = "Payment was already successful. Your account has been created. Please log in.",
                                // We fake the auth URL to be the login page so the UI redirects properly
                                Data = new PayStackInitDto { AuthorizationUrl = "/User/Login" }
                            };
                        }
                    }
                }
            }

            if (draft != null && draft.ExpiresAtUtc < DateTime.UtcNow)
            {
                _registrationDraftRepository.Delete(draft);
                await _unitOfWork.SaveChangesAsync();
                draft = null;
            }

            var response = await _paymentService.IniatilizeRegistrationPayment(email);
            if (response.Status && response.Data != null)
            {
                Session.SetString("Reg_PayRef", response.Data.Reference);

                if (draft != null)
                {
                    draft.CurrentPaymentReference = response.Data.Reference;
                    draft.Status = "PaymentPending";
                    draft.DateUpdatedUtc = DateTime.UtcNow;
                    draft.ExpiresAtUtc = DateTime.UtcNow.AddDays(2);
                    _registrationDraftRepository.Update(draft);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            return response;
        }

        public async Task<BaseResponse<bool>> VerifyPaymentAsync(string? reference = null)
        {
            var referenceVerification = reference ?? Session?.GetString("Reg_PayRef");
            if (string.IsNullOrWhiteSpace(referenceVerification))
            {
                return new BaseResponse<bool> { Status = false, Message = "Payment reference missing." };
            }

            return await ProcessPaymentSuccessAndEnrollAsync(referenceVerification);
        }

        public async Task<BaseResponse<bool>> ProcessPaymentSuccessAndEnrollAsync(
            string reference,
            decimal? paidAmountNaira = null,
            string? paidAt = null)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return new BaseResponse<bool> { Status = false, Message = "Payment reference is required." };
            }

            var sem = _referenceLocks.GetOrAdd(reference, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try
            {
                var payment = await _paymentRepository.GetByReference(reference);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for reference {Reference}", reference);
                return new BaseResponse<bool>
                {
                    Status = false,
                    Message = $"Payment record not found for reference {reference}."
                };
            }

            // 1. Idempotency Check: if student is already created and payment successful, return true
            if (payment.Student != null)
            {
                _logger.LogInformation("Payment {Reference} is already settled and linked to student {StudentId}.", reference, payment.Student.Id);
                var existingDraft = await _registrationDraftRepository.GetByPaymentReferenceAsync(reference);
                if (existingDraft != null)
                {
                    _registrationDraftRepository.Delete(existingDraft);
                    await _unitOfWork.SaveChangesAsync();
                }
                return new BaseResponse<bool> { Status = true, Message = "Registration is already complete." };
            }

            // 2. Settle payment state if not yet marked Successful
            if (!string.Equals(payment.PaymentStatus, "Successful", StringComparison.OrdinalIgnoreCase))
            {
                if (paidAmountNaira.HasValue)
                {
                    if (!_paystack.MatchesExpectedFee(paidAmountNaira.Value))
                    {
                        _logger.LogWarning(
                            "Payment amount {Amount} for {Reference} does not match expected fee {ExpectedFee}.",
                            paidAmountNaira.Value,
                            reference,
                            _paystack.RegistrationFeeNaira);
                        return new BaseResponse<bool> { Status = false, Message = "Payment amount did not match expected fee." };
                    }
                }
                else
                {
                    // Callback verification path: query gateway
                    var verifyResult = await _payStackService.Verify(reference);
                    if (!verifyResult.Status)
                    {
                        return new BaseResponse<bool> { Status = false, Message = verifyResult.Message };
                    }

                    paidAt = verifyResult.Data?.PaidAt;
                    var kobo = verifyResult.Data?.AmountKobo ?? (long)(_paystack.RegistrationFeeNaira * 100m);
                    paidAmountNaira = kobo / 100m;
                }

                payment.PaymentStatus = "Successful";
                payment.PaidAt = paidAt ?? payment.PaidAt ?? DateTime.UtcNow.ToString("O");
                payment.Amount = paidAmountNaira ?? _paystack.RegistrationFeeNaira;
                payment.ConcurrencyStamp = Guid.NewGuid();

                _paymentRepository.Update(payment);
                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Concurrency conflict: another concurrent thread already updated the Payment record.
                    _logger.LogInformation("Concurrent payment status update detected for reference {Reference}; reloading payment state.", reference);
                    payment = await _paymentRepository.GetByReference(reference);
                    if (payment == null)
                    {
                        return new BaseResponse<bool> { Status = false, Message = $"Payment record not found for reference {reference}." };
                    }
                }
            }

            // 3. Complete student registration from draft
            var draft = await _registrationDraftRepository.GetByPaymentReferenceAsync(reference);
            if (draft != null)
            {
                return await CreateFromDraftAsync(draft, reference);
            }

            // Fallback for legacy session-backed registrations
            string? email = null;
            try
            {
                email = Session?.GetString("Email");
            }
            catch
            {
                // Non-HTTP context (e.g. background task)
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                draft = await _registrationDraftRepository.GetByEmailAsync(email);
                if (draft != null)
                {
                    return await CreateFromDraftAsync(draft, reference);
                }

                return await CreateAsync();
            }

            // If payment is already Successful and draft was already converted, verify user exists
            return new BaseResponse<bool> { Status = true, Message = "Payment confirmed successfully." };
            }
            finally
            {
                sem.Release();
                if (sem.CurrentCount == 1)
                {
                    _referenceLocks.TryRemove(new KeyValuePair<string, SemaphoreSlim>(reference, sem));
                }
            }
        }

        public async Task<BaseResponse<bool>> StepOneRegistration(CreateStudentRequestModel model)
        {
            if (model == null)
            {
                _logger.LogError("User Must Input all values");
                return new BaseResponse<bool> { Status = false, Message = "Invalid details provided." };
            }

            if (model.Password != model.ConfirmPassword)
            {
                return new BaseResponse<bool> { Message = "Password doesn't match!", Status = false };
            }

            (var passwordResult, var message) = ValidatePassword(model.Password);
            if (!passwordResult)
            {
                return new BaseResponse<bool> { Message = message, Status = false };
            }

            var studentExists = await _userRepository.Any(x => x.Email == model.Email);
            if (studentExists)
            {
                _logger.LogError("Student with email already exists");
                return new BaseResponse<bool> { Message = "Student with this email already exists", Status = false };
            }

            Session.SetString("FirstName", model.FirstName);
            Session.SetString("LastName", model.LastName);
            Session.SetString("Gender", model.Gender.ToString());
            Session.SetString("PhoneNumber", model.PhoneNumber);
            Session.SetString("City", model.City);
            Session.SetString("State", model.State);
            Session.SetString("Address", model.Address);
            Session.SetString("Nin", model.NIN);
            Session.SetString("Email", model.Email);
            Session.SetString("MiddleName", model.MiddleName ?? string.Empty);
            Session.SetString("Password", model.Password);
            Session.SetString("ConfirmPassword", model.ConfirmPassword);
            Session.SetString("DateOfBirth", model.DateOfBirth.ToString());
            Session.SetString("DeptId", model.DepartmentId.ToString());

            var now = DateTime.UtcNow;
            var draft = await _registrationDraftRepository.GetByEmailAsync(model.Email);
            var isNewDraft = false;
            if (draft == null)
            {
                isNewDraft = true;
                draft = new RegistrationDraft
                {
                    Email = model.Email,
                    DateCreated = now,
                };
                await _registrationDraftRepository.Add(draft);
            }

            draft.FirstName = model.FirstName;
            draft.LastName = model.LastName;
            draft.MiddleName = model.MiddleName ?? string.Empty;
            draft.Gender = model.Gender;
            draft.PhoneNumber = model.PhoneNumber;
            draft.City = model.City;
            draft.State = model.State;
            draft.Address = model.Address;
            draft.NIN = model.NIN;
            draft.DateOfBirth = model.DateOfBirth;
            draft.DepartmentId = model.DepartmentId;
            draft.PasswordHash = _identityService.GetPasswordHash(model.Password);
            draft.Status = "Step1";
            draft.DateUpdatedUtc = now;
            draft.ExpiresAtUtc = now.AddDays(2);
            draft.IsDelete = false;

            if (!isNewDraft)
            {
                _registrationDraftRepository.Update(draft);
            }
            await _unitOfWork.SaveChangesAsync();

            Session.SetString("DraftId", draft.Id.ToString());

            return new BaseResponse<bool> { Status = true, Message = "Step 1 details are valid." };
        }

        public async Task StepTwoRegistration(CreateNewWaecModel waecModel, CreatePassportRequestModel passportModel)
        {
            Session.SetString("ExamNo", waecModel.ExaminationNumber);
            Session.SetString("Year", waecModel.ExaminationYear);
            Session.SetString("ExamBody", waecModel.ExaminationBody ?? string.Empty);
            Session.SetString("SecondExamBody", waecModel.SecondExaminationBody ?? string.Empty);
            Session.SetString("WaecResultDetailsJson", SerializeWaecResultDetails(waecModel));

            string? passportPath = null;

            if (passportModel.PassportPhoto != null)
            {
                passportPath = await SaveFileToDisk(passportModel.PassportPhoto, "passports");
                Session.SetString("passports", passportPath);
            }

            var draft = await TryGetDraftFromSessionAsync();
            if (draft == null)
            {
                _logger.LogWarning("Registration draft not found while saving step two.");
                return;
            }

            draft.ExamNo = waecModel.ExaminationNumber ?? string.Empty;
            draft.ExamYear = waecModel.ExaminationYear ?? string.Empty;
            draft.ExamBody = waecModel.ExaminationBody;
            draft.SecondExamBody = waecModel.SecondExaminationBody;
            draft.WaecResultDetailsJson = SerializeWaecResultDetails(waecModel);
            if (!string.IsNullOrWhiteSpace(passportPath)) draft.PassportPath = passportPath;

            draft.Status = "Step2";
            draft.DateUpdatedUtc = DateTime.UtcNow;
            draft.ExpiresAtUtc = DateTime.UtcNow.AddDays(2);

            _registrationDraftRepository.Update(draft);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<BaseResponse<StudentDtos>> GetByIdAsync(Guid studentId, CancellationToken cancellationToken)
        {
            var getCustomer = await _studentRepository.GetStudentByStudentId(studentId);
            if (getCustomer == null)
            {
                _logger.LogError("Student doesn't exist");
                return new BaseResponse<StudentDtos>
                {
                    Message = "Student doesn't exist",
                    Status = false
                };
            }
            return new BaseResponse<StudentDtos>
            {
                Message = "Student Found Successfully",
                Status = true,
                Data = new StudentDtos
                {
                    DateCreated = getCustomer.DateCreated,
                    FirstName = getCustomer.FirstName,
                    LastName = getCustomer.LastName,
                    Address = getCustomer.Address,
                    MatricNumber = getCustomer.MatricNumber,
                    City = getCustomer.City,
                    DateOfBirth = getCustomer.DateOfBirth,
                    Email = getCustomer.Email,
                     Gender = getCustomer.Gender,
                     DepartmentId = getCustomer.DepartmentId,
                     DepartmentName = getCustomer.Department?.DepartmentName,
                    RejectionReason = getCustomer.RejectionReason,
                    RegistrationNumber = getCustomer.RegistrationNumber,
                    Status = getCustomer.Status,
                    NIN = getCustomer.NIN,
                    MiddleName = getCustomer.MiddleName,
                    PhoneNumber = getCustomer.PhoneNumber,
                    State = getCustomer.State,
                    ApprovedDate = getCustomer.ApprovedDate,
                    PassportUrl = getCustomer.Passport.PassportUrl,
                    WaecDetails = ParseWaecDetails(getCustomer.Waec),
                    Id = getCustomer.Id
                }
            };
        }

        public async Task<BaseResponse<StudentDashboardDtos>> StudentDashboard(Guid studentId, CancellationToken cancellationToken)
        {
            var getCustomer = await _studentRepository.GetStudentByIdAsync(studentId);
            if (getCustomer == null)
            {
                _logger.LogError("Student doesn't exist");
                return new BaseResponse<StudentDashboardDtos>
                {
                    Message = "Student doesn't exist",
                    Status = false
                };
            }
            return new BaseResponse<StudentDashboardDtos>
            {
                Message = "Student Found Successfully",
                Status = true,
                Data = new StudentDashboardDtos
                {
                    FirstName = getCustomer.FirstName,
                    LastName = getCustomer.LastName,
                    City = getCustomer.City,
                    Email = getCustomer.Email,
                    Gender = getCustomer.Gender,
                    Status = getCustomer.Status,
                    MiddleName = getCustomer.MiddleName,
                    PhoneNumber = getCustomer.PhoneNumber,
                    State = getCustomer.State,
                }
            };
        }

        public async Task<BaseResponse<bool>> DeleteAsync(Guid studentId, CancellationToken cancellationToken)
        {
            var getStudent = await _studentRepository.GetStudentByStudentId(studentId);
            if (getStudent == null)
            {
                _logger.LogError("Student with id not found");
                return new BaseResponse<bool>()
                {
                    Status = false,
                    Message = "Student with id not found"
                };
            }
            var deleteStudent = await _studentRepository.DeleteStudentById(studentId);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            return new BaseResponse<bool>
            {
                Status = true,
                Message = "Student Deleted Successfully"
            };
        }

        public async Task<BaseResponse<IEnumerable<StudentDtos>>> StudentAsync()
        {
            var getDepartments = await _studentRepository.GetAll<Student>();
            if (getDepartments == null)
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
                    Id = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.Email,
                    DateCreated = s.DateCreated,
                    State = s.State,
                    DateOfBirth = s.DateOfBirth,
                    Gender = s.Gender,
                    Status = s.Status
                }).ToList()
            };
        }

        private async Task<RegistrationDraft?> TryGetDraftFromSessionAsync()
        {
            var draftIdRaw = Session.GetString("DraftId");
            if (Guid.TryParse(draftIdRaw, out var draftId))
            {
                var byId = await _registrationDraftRepository.GetByIdAsync(draftId);
                if (byId != null) return byId;
            }

            var email = Session.GetString("Email");
            if (!string.IsNullOrWhiteSpace(email))
            {
                return await _registrationDraftRepository.GetByEmailAsync(email);
            }

            return null;
        }

        private async Task<BaseResponse<bool>> CreateFromDraftAsync(RegistrationDraft draft, string paymentReference)
        {
            if (string.IsNullOrWhiteSpace(draft.Email) || string.IsNullOrWhiteSpace(draft.PasswordHash))
            {
                return new BaseResponse<bool> { Status = false, Message = "Registration draft is incomplete. Restart registration." };
            }

            if (draft.DepartmentId == Guid.Empty)
            {
                return new BaseResponse<bool> { Status = false, Message = "Department is missing. Restart registration." };
            }

            if (string.IsNullOrWhiteSpace(draft.PassportPath))
            {
                return new BaseResponse<bool> { Status = false, Message = "Passport not uploaded. Complete step two and try again." };
            }

            if (string.IsNullOrWhiteSpace(draft.ExamNo) || string.IsNullOrWhiteSpace(draft.ExamYear) || !GetCompletedWaecResults(draft.WaecResultDetailsJson).Any())
            {
                return new BaseResponse<bool> { Status = false, Message = "WAEC result details are incomplete. Complete step two and try again." };
            }

            var payment = await _paymentRepository.GetByReference(paymentReference);
            if (payment == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Payment record not found for reference." };
            }

            // If payment already has a linked student, we are already done!
            if (payment.Student != null)
            {
                _registrationDraftRepository.Delete(draft);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                return new BaseResponse<bool> { Status = true, Message = "Student already registered." };
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(draft.Email);
                User studentUser;

                if (existingUser != null)
                {
                    studentUser = existingUser;
                    var existingStudent = await _studentRepository.Get<Student>(s => s.UserId == existingUser.Id);
                    if (existingStudent != null)
                    {
                        if (existingStudent.PaymentId == Guid.Empty || existingStudent.PaymentId != payment.Id)
                        {
                            existingStudent.Payment = payment;
                            payment.Student = existingStudent;
                            _studentRepository.Update(existingStudent);
                            _paymentRepository.Update(payment);
                        }

                        _registrationDraftRepository.Delete(draft);
                        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                        await transaction.CommitAsync();
                        return new BaseResponse<bool> { Status = true, Message = "Student Added Successfully" };
                    }
                }
                else
                {
                    studentUser = new User
                    {
                        Email = draft.Email,
                        DateCreated = DateTime.UtcNow,
                        PasswordHash = draft.PasswordHash
                    };

                    try
                    {
                        var createResult = await _userManager.CreateAsync(studentUser);
                        if (createResult == null || !createResult.Succeeded)
                        {
                            // Check if created concurrently by another thread
                            existingUser = await _userManager.FindByEmailAsync(draft.Email);
                            if (existingUser != null)
                            {
                                studentUser = existingUser;
                            }
                            else
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
                        }
                    }
                    catch (DbUpdateException)
                    {
                        // Concurrent insert collision; fetch the newly created user
                        existingUser = await _userManager.FindByEmailAsync(draft.Email);
                        if (existingUser != null)
                        {
                            studentUser = existingUser;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    var isInRole = await _userManager.IsInRoleAsync(studentUser, "Student");
                    if (!isInRole)
                    {
                        var addRoleResult = await _userManager.AddToRoleAsync(studentUser, "Student");
                        if (!addRoleResult.Succeeded)
                        {
                            var errors = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                            _logger.LogWarning("Unable to add user to role: {Errors}", errors);
                        }
                    }
                }

                var student = new Student
                {
                    UserId = studentUser.Id,
                    DepartmentId = draft.DepartmentId,
                    FirstName = draft.FirstName,
                    MiddleName = draft.MiddleName,
                    RegistrationNumber = RegistrationNumber(),
                    Status = Status.Pending,
                    State = draft.State,
                    LastName = draft.LastName,
                    Address = draft.Address,
                    City = draft.City,
                    DateCreated = DateTime.UtcNow,
                    DateOfBirth = draft.DateOfBirth,
                    Gender = draft.Gender ?? Gender.Other,
                    Email = draft.Email,
                    IsAdmitted = false,
                    NIN = draft.NIN,
                    PhoneNumber = draft.PhoneNumber,
                    Passport = new Passport
                    {
                        PassportUrl = draft.PassportPath,
                        DateCreated = DateTime.UtcNow,
                    },
                    Waec = new Waec
                    {
                        ExaminationNumber = draft.ExamNo,
                        ExaminationYear = draft.ExamYear,
                        ExaminationBody = draft.ExamBody,
                        SecondExaminationBody = draft.SecondExamBody,
                        ResultDetailsJson = draft.WaecResultDetailsJson,
                        DateCreated = DateTime.UtcNow,
                    }
                };

                student.Payment = payment;
                payment.Student = student;

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

                _registrationDraftRepository.Delete(draft);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);

                await transaction.CommitAsync();
                return new BaseResponse<bool> { Status = true, Message = "Student Added Successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student from draft, rolling back.");
                try { await transaction.RollbackAsync(); } catch (Exception rbEx) { _logger.LogError(rbEx, "Rollback failed"); }

                // Check if concurrent thread already created the student
                var confirmedStudent = await _studentRepository.Get<Student>(s => s.Email == draft.Email);
                if (confirmedStudent != null)
                {
                    _logger.LogInformation("Concurrent registration completed successfully for {Email}", draft.Email);
                    return new BaseResponse<bool> { Status = true, Message = "Student Added Successfully" };
                }

                return new BaseResponse<bool> { Status = false, Message = "An error occurred while creating Student" };
            }
        }

        private async Task<string> SaveFileToDisk(IFormFile file, string subFolder)
        {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string uploadDir = Path.Combine(_env.WebRootPath, "uploads", subFolder);

            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
            string fullPath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
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
            int minLength = 8;
            int maxLength = 50;

            if (string.IsNullOrEmpty(password))
            {
                return (false, "Password cannot be null or empty.");
            }

            if (password.Length < minLength || password.Length > maxLength)
            {
                return (false, $"Password must be between {minLength} and {maxLength} characters long.");
            }

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

            string invalidCharacters = @" !""#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
            if (password.IndexOfAny(invalidCharacters.ToCharArray()) == -1)
            {
                return (false, "Password must contain one or more characters.");
            }

            return (true, null);
        }

        public async Task<BaseResponse<bool>> UpdateAdmissionStatus(Guid id, UpdateStudentAdmissionRequestModel model)
        {
            var getStudent = await _studentRepository.GetStudentForAdmissionUpdateAsync(id);
            if (getStudent == null)
            {
                _logger.LogError("Student Not Found");
                return new BaseResponse<bool> { Status = false , Message="Student Not Found" };
            }
            if(model == null)
            {
                return new BaseResponse<bool>
                {
                    Message = "Fields cannot be empty",
                    Status = false,
                };
            }

            // Enforce finality rules
            if (getStudent.Status == Status.Admitted && model.Status == Status.Rejected)
            {
                return new BaseResponse<bool>
                {
                    Status = false,
                    Message = "Cannot change status to Rejected once the applicant has been Admitted."
                };
            }
            if (getStudent.Status == Status.Rejected && model.Status == Status.Admitted)
            {
                return new BaseResponse<bool>
                {
                    Status = false,
                    Message = "Cannot change status to Admitted once the applicant has been Rejected."
                };
            }

            getStudent.Status = model.Status;
            if (model.DepartmentId != Guid.Empty)
            {
                getStudent.DepartmentId = model.DepartmentId;
                getStudent.Department = await _studentRepository.Get<Department>(department => department.Id == model.DepartmentId);
            }

            if (model.Status == Status.Admitted)
            {
                getStudent.IsAdmitted = true;
                getStudent.ApprovedDate = DateTime.UtcNow;
                getStudent.RejectedDate = null;
            }
            else if (model.Status == Status.Rejected)
            {
                getStudent.IsAdmitted = false;
                getStudent.RejectedDate = DateTime.UtcNow;
                getStudent.ApprovedDate = null;
            }
            else
            {
                getStudent.IsAdmitted = false;
            }
            _studentRepository.Update(getStudent);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            var studentEmail = getStudent.Email;
            if (string.IsNullOrWhiteSpace(studentEmail))
            {
                studentEmail = getStudent.User?.Email;
            }

            if (string.IsNullOrWhiteSpace(studentEmail))
            {
                _logger.LogWarning("Admission status updated but email was not sent because student email is missing. StudentId: {StudentId}", id);
            }
            else
            {
                try
                {
                    var subject = getStudent.Status switch
                    {
                        Status.Admitted => "Admission Offer",
                        Status.Rejected => "Application Decision Notice",
                        _ => "Application Update"
                    };

                    await _mailService.SendAdmissionStatus(
                        studentEmail,
                        getStudent.FullName(),
                        subject,
                        getStudent.Status.ToString(),
                        getStudent.Department?.DepartmentName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Admission status updated but failed to send email. StudentId: {StudentId}", id);
                }
            }

            return new BaseResponse<bool>
            {
                Message = "Student Admission Updated Successfully",
                Status = true
            };

        }

        private static IEnumerable<WaecSubjectResultEntryModel> GetCompletedWaecResults(string? resultDetailsJson)
        {
            if (string.IsNullOrWhiteSpace(resultDetailsJson))
            {
                return [];
            }

            try
            {
                var details = JsonSerializer.Deserialize<WaecResultDetailsModel>(resultDetailsJson);
                return details?.SubjectResults?
                    .Where(result => !string.IsNullOrWhiteSpace(result.Subject) || !string.IsNullOrWhiteSpace(result.Grade))
                    .ToList() ?? [];
            }
            catch
            {
                return [];
            }
        }

        private static string SerializeWaecResultDetails(CreateNewWaecModel waecModel)
        {
            var details = new WaecResultDetailsModel
            {
                SittingType = waecModel.SittingType,
                ExaminationBody = waecModel.ExaminationBody?.Trim(),
                SecondExaminationBody = waecModel.SittingType == ResultSittingType.TwoSittings ? waecModel.SecondExaminationBody?.Trim() : null,
                SecondExaminationYear = waecModel.SittingType == ResultSittingType.TwoSittings ? waecModel.SecondExaminationYear?.Trim() : null,
                SecondExaminationNumber = waecModel.SittingType == ResultSittingType.TwoSittings ? waecModel.SecondExaminationNumber?.Trim() : null,
                SubjectResults = (waecModel.SubjectResults ?? [])
                    .Where(result => !string.IsNullOrWhiteSpace(result.Subject) || !string.IsNullOrWhiteSpace(result.Grade))
                    .Select(result => new WaecSubjectResultEntryModel
                    {
                        Subject = result.Subject?.Trim(),
                        Grade = result.Grade?.Trim()?.ToUpperInvariant(),
                        SittingNumber = waecModel.SittingType == ResultSittingType.OneSitting ? 1 : result.SittingNumber
                    })
                    .ToList()
            };

            return JsonSerializer.Serialize(details);
        }

        private static Mitc_Registration_Portal.Models.DTOs.Waecs.CreateNewWaecModel? ParseWaecDetails(Mitc_Registration_Portal.Models.Entities.Waec? waec)
        {
            if (waec == null) return null;

            var waecDetails = new Mitc_Registration_Portal.Models.DTOs.Waecs.CreateNewWaecModel
            {
                ExaminationNumber = waec.ExaminationNumber ?? string.Empty,
                ExaminationYear = waec.ExaminationYear ?? string.Empty,
                ExaminationBody = waec.ExaminationBody,
                SecondExaminationBody = waec.SecondExaminationBody,
                SubjectResults = []
            };

            if (!string.IsNullOrWhiteSpace(waec.ResultDetailsJson))
            {
                try
                {
                    var details = JsonSerializer.Deserialize<Mitc_Registration_Portal.Models.DTOs.Waecs.WaecResultDetailsModel>(waec.ResultDetailsJson);
                    if (details != null)
                    {
                        waecDetails.SittingType = details.SittingType;
                        waecDetails.ExaminationBody ??= details.ExaminationBody;
                        waecDetails.SecondExaminationBody ??= details.SecondExaminationBody;
                        waecDetails.SecondExaminationNumber = details.SecondExaminationNumber;
                        waecDetails.SecondExaminationYear = details.SecondExaminationYear;
                        waecDetails.SubjectResults = details.SubjectResults ?? [];
                    }
                }
                catch
                {
                    // Ignore deserialization errors
                }
            }

            return waecDetails;
        }
    }
}
