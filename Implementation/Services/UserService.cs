using Microsoft.AspNetCore.Identity;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Roles;
using Mitc_Registration_Portal.Models.DTOs.Users;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Implementation.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentService _studentService;

        public UserService(IUserRepository userRepository,
            UserManager<User> userManager,
            ILogger<UserService> logger,
            IUnitOfWork unitOfWork,
            IStudentService studentService
            )
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _studentService = studentService;

        }
        public async Task<BaseResponse<UserDtos>> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogError("User With Email Not Found");
                return new BaseResponse<UserDtos>
                {
                    Message = "User With Email Not Found",
                    Status = false
                };
            }
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;
            if (role == "Customer")
            {
                return new BaseResponse<UserDtos>
                {
                    Message = "Customer profile fetched",
                    Status = true,
                    Data = new UserDtos
                    {

                        Id = user.Id,
                        Email = user.Email,
                        Roles = roles.Select(r => new RoleDtos { Name = r }).ToList(),
                        Student = new Models.DTOs.Students.StudentDtos
                        {
                            Id = user.Student.Id,
                            FirstName = user.Student.FirstName,
                            Gender = user.Student.Gender,
                            PhoneNumber = user.Student.PhoneNumber,
                            DateOfBirth = user.Student.DateOfBirth,
                            DateCreated = user.Student.DateCreated,
                            Address = user.Student.Address,
                            City = user.Student.City,
                            


                        }
                    }
                };
            }

            return new BaseResponse<UserDtos>
            {
                Message = "Admin profile fetched",
                Status = true,
                Data = new UserDtos
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = role.Select(r => new RoleDtos { Name = role }).ToList(),
                    Admin = new AdminDto
                    {
                        FullName = $"{user.Admin.FirstName} {user.Admin.LastName}"

                    }

                }
            };

        }
        public async Task<BaseResponse<UserDtos>> GetUserProfileByUserId(Guid userId, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserProfile(userId);
            if (user == null)
            {
                return new BaseResponse<UserDtos>
                {
                    Message = "User not found",
                    Status = false
                };
            }
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;

            if (role == "Customer")
            {
                return new BaseResponse<UserDtos>
                {
                    Message = "Customer profile fetched",
                    Status = true,
                    Data = new UserDtos
                    {

                        Id = user.Id,
                        Email = user.Email,
                        Roles = roles.Select(r => new RoleDtos { Name = r }).ToList(),
                        Student = new Models.DTOs.Students.StudentDtos
                        {
                            Id = user.Student.Id,
                            FirstName = user.Student.FirstName,
                            Gender = user.Student.Gender,
                            PhoneNumber = user.Student.PhoneNumber,
                            DateOfBirth = user.Student.DateOfBirth,
                            DateCreated = user.Student.DateCreated,
                            Address = user.Student.Address,

                        }
                    }
                };
            }

            return new BaseResponse<UserDtos>
            {
                Message = "Admin profile fetched",
                Status = true,
                Data = new UserDtos
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = role.Select(r => new RoleDtos { Name = role }).ToList(),
                    Admin = new AdminDto
                    {
                        FullName = $"{user.Admin.FirstName} {user.Admin.LastName}"

                    }

                }
            };


        }

        public async Task<BaseResponse<LoginResponseModel>> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByEmail(request.Email);
            if (user == null)
            {
                _logger.LogError("Invalid credentials");
                return new BaseResponse<LoginResponseModel>
                {
                    Message = "Invalid credentials",
                    Status = false
                };
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                _logger.LogError("Invalid credentials");
                return new BaseResponse<LoginResponseModel>
                {
                    Message = "Invalid credentials",
                    Status = false
                };
            }
            var roles = await _userManager.GetRolesAsync(user);

            var role = roles.FirstOrDefault() ?? string.Empty;

            if (role == "Student")
            {
                return new BaseResponse<LoginResponseModel>
                {
                    Message = "Login successful",
                    Status = true,
                    Data = new LoginResponseModel
                    {

                        UserId = user.Id,
                        Email = user.Email,
                        Roles = roles.Select(r => new RoleDtos { Name = r }).ToList(),
                        FirstName = user.Student != null ? $"{user.Student.FirstName}" : string.Empty,
                        FullName = user.Admin != null ? $"{user.Student?.FullName()}" : string.Empty

                    }
                };
            }

            return new BaseResponse<LoginResponseModel>
            {
                Message = "Login successful",
                Status = true,
                Data = new LoginResponseModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = role.Select(r => new RoleDtos { Name = role }).ToList(),
                    FirstName = user.Admin != null ? $"{user.Admin.FirstName}" : string.Empty,
                    FullName = user.Admin != null ? $"{user.Admin.FullName()}" : string.Empty,
                }
            };
        }

    }
}
