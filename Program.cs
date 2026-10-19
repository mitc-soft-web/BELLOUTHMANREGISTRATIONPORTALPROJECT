using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Polly;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Interface.Services;
using Mitc_Registration_Portal.Contracts.MailingService;
using Mitc_Registration_Portal.Contracts.Messaging;
using Mitc_Registration_Portal.Identity;
using Mitc_Registration_Portal.Implementation.MailingService;
using Mitc_Registration_Portal.Implementation.Repositories;
using Mitc_Registration_Portal.Implementation.Services;
using Mitc_Registration_Portal.Implementation.TemplateEngine;
using Mitc_Registration_Portal.Interface.Repositories;
using Mitc_Registration_Portal.Interface.TemplateEngine;
using Mitc_Registration_Portal.Messaging;
using Mitc_Registration_Portal.Models.DTOs.Students.Validation;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddOptions<PaystackConfiguration>()
    .Bind(builder.Configuration.GetSection(PaystackConfiguration.SectionName))
    .Validate(
        o => o.RegistrationFeeNaira > 0,
        "Paystack:RegistrationFeeNaira must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IPayStackService, PayStackService>(client =>
{
    client.BaseAddress = new Uri("https://api.paystack.co/");
})
.AddStandardResilienceHandler(options =>
{
    // Fast-fail attempt timeout: 3.5 seconds for connection/DNS
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3.5);

    // Total timeout across all attempts
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);

    // Exponential backoff with jitter on transient errors (502, 503, 504, 408)
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.Delay = TimeSpan.FromMilliseconds(500);

    // Circuit breaker: trips if >= 50% of requests fail within 30s window (min 5 requests)
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient();

builder.Services.Scan(scan => scan
    .FromApplicationDependencies(a => a.FullName!.StartsWith("Mitc_Registration_Portal"))
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Service") && t.Name != nameof(PayStackService)))
        .AsImplementedInterfaces()
        .WithScopedLifetime()
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository")))
        .AsImplementedInterfaces()
        .WithScopedLifetime());

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Hosted worker for TTL cleanup (registered explicitly as singleton).
builder.Services.AddHostedService<RegistrationDraftCleanupWorker>();

builder.Services.AddValidatorsFromAssemblyContaining<StudentValidation>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddDbContext<StudentDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("MITCContext"),
        new MySqlServerVersion(new Version(8, 0, 36))
    ));

builder.Services.AddScoped<IUserStore<User>, Mitc_Registration_Portal.Identity.UserStore>();
builder.Services.AddScoped<IRoleStore<Role>, RoleStore>();
builder.Services.AddIdentity<User, Role>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IMailSender, MailSender>();
builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection("EmailConfiguration"));
builder.Services.AddScoped<IRazorEngine, RazorEngine>();


builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(config =>
    {
        config.LoginPath = "/User/login";
        config.Cookie.Name = "Mitc_Registration_Portal";
        config.LogoutPath = "/User/logout";
        config.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        config.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply migrations automatically on startup so database schema and seeds are created on new deployments.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Applying database migrations...");
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

