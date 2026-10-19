using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Contracts.MailingService;
using Mitc_Registration_Portal.Contracts.Messaging;
using Mitc_Registration_Portal.Contracts.Services;
using Mitc_Registration_Portal.Identity;
using Mitc_Registration_Portal.Implementation.Repositories;
using Mitc_Registration_Portal.Implementation.Services;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Models.DTOs;
using Mitc_Registration_Portal.Models.DTOs.Payments;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Models.Enums;
using Mitc_Registration_Portal.Persistence.Context;
using Xunit;

namespace Mitc_Registration_Portal.Tests
{
    public class PaymentIdempotencyAndConcurrencyTests
    {
        private class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; } = AppContext.BaseDirectory;
            public IFileProvider WebRootFileProvider { get; set; } = null!;
            public string ApplicationName { get; set; } = "Mitc_Registration_Portal";
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public string EnvironmentName { get; set; } = "Testing";
        }

        private class MockMailService : IMailService
        {
            public Task<bool> SendAdmissionStatus(string email, string name, string title, string status, string? departmentName) =>
                Task.FromResult(true);
        }

        private class MockPayStackService : IPayStackService
        {
            public Task<BaseResponse<PayStackInitDto>> Initialize(string reference, decimal amount, string email) =>
                Task.FromResult(new BaseResponse<PayStackInitDto>
                {
                    Status = true,
                    Message = "Initialized",
                    Data = new PayStackInitDto
                    {
                        Reference = reference,
                        AuthorizationUrl = "https://checkout.paystack.com/test-auth"
                    }
                });

            public Task<BaseResponse<PayStackVerifyDto>> Verify(string reference) =>
                Task.FromResult(new BaseResponse<PayStackVerifyDto>
                {
                    Status = true,
                    Message = "Payment Verified",
                    Data = new PayStackVerifyDto
                    {
                        Status = "success",
                        PaidAt = "2026-10-18T10:00:00Z",
                        Channel = "card",
                        AmountKobo = 500000
                    }
                });
        }

        private ServiceProvider BuildServiceProvider(string dbName)
        {
            var services = new ServiceCollection();

            services.AddDbContext<StudentDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: dbName);
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configuration);

            services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
            services.AddHttpContextAccessor();

            services.AddScoped<IUserStore<User>, UserStore>();
            services.AddScoped<IRoleStore<Role>, RoleStore>();
            services.AddIdentity<User, Role>()
                .AddDefaultTokenProviders();

            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRegistrationDraftRepository, RegistrationDraftRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddSingleton<IWebHostEnvironment, TestWebHostEnvironment>();
            services.AddScoped<IMailService, MockMailService>();

            services.Configure<PaystackConfiguration>(opts =>
            {
                opts.SecretKey = "sk_test_mock_secret_key_12345";
                opts.CallbackUrl = "https://localhost:7082/Payment/Callback";
                opts.RegistrationFeeNaira = 5000.00m;
            });

            services.AddScoped<IPayStackService, MockPayStackService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IStudentService, StudentService>();

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task Concurrent_CallbackAndWebhook_ProcessesIdempotentlyWithoutDuplicatesOrErrors()
        {
            // Arrange
            var dbName = "PaymentIdempotencyDb_" + Guid.NewGuid();
            var serviceProvider = BuildServiceProvider(dbName);

            var deptId = Guid.NewGuid();
            var testReference = "REG-CONCURRENT-001";
            var testEmail = "concurrency.student@test.com";

            // Seed required Department, Role, Payment and RegistrationDraft
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
                await db.Database.EnsureCreatedAsync();

                var dept = new Department
                {
                    Id = deptId,
                    DepartmentName = "Computer Science",
                    DepartmentalCode = "CSC",
                    DateCreated = DateTime.UtcNow
                };
                db.Set<Department>().Add(dept);

                var payment = new Payment
                {
                    Reference = testReference,
                    Amount = 5000.00m,
                    PaymentStatus = "Pending",
                    DateCreated = DateTime.UtcNow
                };
                db.Set<Payment>().Add(payment);

                var draft = new RegistrationDraft
                {
                    Email = testEmail,
                    FirstName = "Grace",
                    LastName = "Hopper",
                    MiddleName = "B",
                    PhoneNumber = "+2348011223344",
                    City = "Lagos",
                    State = "Lagos",
                    Address = "12 Tech Lane",
                    NIN = "12345678901",
                    DateOfBirth = new DateTime(2001, 1, 1),
                    DepartmentId = deptId,
                    PasswordHash = "AQAAAAIAAYagAAAAEJjieFsJGM2Xgr+WpuS3juOABbBCvbqSvpym4WzP/SDMuvGz6qH+EFgm19l8SUHUGA==",
                    CurrentPaymentReference = testReference,
                    PassportPath = "/uploads/passports/grace.jpg",
                    ExamNo = "99887766",
                    ExamYear = "2024",
                    ExamBody = "WAEC",
                    WaecResultDetailsJson = "{\"SittingType\":1,\"ExaminationBody\":\"WAEC\",\"SubjectResults\":[{\"Subject\":\"Mathematics\",\"Grade\":\"A1\",\"SittingNumber\":1},{\"Subject\":\"English\",\"Grade\":\"B2\",\"SittingNumber\":1}]}",
                    Status = "Step2",
                    DateCreated = DateTime.UtcNow,
                    DateUpdatedUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(2)
                };
                db.Set<RegistrationDraft>().Add(draft);

                await db.SaveChangesAsync();
            }

            // Act: Fire 10 simultaneous requests across concurrent scopes
            // 5 simulating Paystack Webhook (ProcessPaymentSuccessAndEnrollAsync with direct amount)
            // 5 simulating User Browser Callback (VerifyPaymentAsync)
            var tasks = new List<Task<BaseResponse<bool>>>();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = serviceProvider.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IStudentService>();
                    return await svc.ProcessPaymentSuccessAndEnrollAsync(
                        testReference,
                        5000.00m,
                        "2026-10-18T12:00:00Z");
                }));

                tasks.Add(Task.Run(async () =>
                {
                    using var scope = serviceProvider.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IStudentService>();
                    return await svc.VerifyPaymentAsync(testReference);
                }));
            }

            var results = await Task.WhenAll(tasks);

            // Assert: All 10 operations must report success
            foreach (var res in results)
            {
                Assert.NotNull(res);
                Assert.True(res.Status, $"Operation failed with message: {res.Message}");
            }

            // Verify final database state: exactly 1 student, 1 user, 1 successful payment, 0 drafts
            using (var verifyScope = serviceProvider.CreateScope())
            {
                var db = verifyScope.ServiceProvider.GetRequiredService<StudentDbContext>();

                var savedPayment = await db.Set<Payment>()
                    .Include(p => p.Student)
                    .FirstOrDefaultAsync(p => p.Reference == testReference);

                Assert.NotNull(savedPayment);
                Assert.Equal("Successful", savedPayment.PaymentStatus);
                Assert.Equal(5000.00m, savedPayment.Amount);
                Assert.NotNull(savedPayment.Student);

                var usersWithEmail = await db.Set<User>()
                    .Where(u => u.Email == testEmail)
                    .ToListAsync();
                Assert.Single(usersWithEmail);

                var studentsWithEmail = await db.Set<Student>()
                    .Where(s => s.Email == testEmail)
                    .ToListAsync();
                Assert.Single(studentsWithEmail);

                var remainingDrafts = await db.Set<RegistrationDraft>()
                    .Where(d => d.CurrentPaymentReference == testReference)
                    .ToListAsync();
                Assert.Empty(remainingDrafts);
            }
        }
    }
}
