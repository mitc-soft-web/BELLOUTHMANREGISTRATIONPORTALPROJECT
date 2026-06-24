using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Persistence.Context
{
    public class StudentDbContext : DbContext
    {
        public StudentDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Admin>()
                .Property(a => a.Gender)
                .HasConversion<byte>();

            SeedAdminData(builder);
            SeedRoleData(builder);

            builder.Entity<Student>()
                .HasOne(c => c.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            builder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            builder.Entity<Department>()
                .HasMany(s => s.Students)
                .WithOne(d => d.Department)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Student>()
                .HasOne(d => d.Passport)
                .WithOne(s => s.Student)
                .HasForeignKey<Student>(s => s.PassportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Student>()
                .HasOne(d => d.Waec)
                .WithOne(s => s.Student)
                .HasForeignKey<Student>(s => s.WaecId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Student>()
                .HasOne(d => d.Payment)
                .WithOne(s => s.Student)
                .HasForeignKey<Student>(s => s.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            // In-progress registration state for idempotent retries + payment callback reliability.
            builder.Entity<RegistrationDraft>()
                .Property(d => d.Email)
                .HasMaxLength(255);

            builder.Entity<RegistrationDraft>()
                .Property(d => d.CurrentPaymentReference)
                .HasMaxLength(100);

            builder.Entity<RegistrationDraft>()
                .HasIndex(d => d.Email)
                .IsUnique();

            builder.Entity<RegistrationDraft>()
                .HasIndex(d => d.CurrentPaymentReference)
                .IsUnique();

            builder.Entity<RegistrationDraft>()
                .Property(d => d.Gender)
                .HasConversion<byte?>();
        }

        private void SeedAdminData(ModelBuilder modelBuilder)
        {
            var adminRoleId = new Guid("d2719e67-52f4-4f9c-bdb2-123456789abc");
            var adminUserId = new Guid("c8f2e5ab-9f34-4b97-8b7c-1a5e86c77e42");

            var role = new Role
            {
                Id = adminRoleId,
                Name = "Admin",
                Description = "Has full permissions",
                DateCreated = DateTime.SpecifyKind(new DateTime(2025, 11, 10), DateTimeKind.Utc)
            };

            var hasher = new PasswordHasher<object>();
            var passwordHash = hasher.HashPassword(null, "Admin@001");

            var adminUser = new User
            {
                Id = adminUserId,
                Email = "Admin001@gmail.com",
                PasswordHash = "AQAAAAIAAYagAAAAEJjieFsJGM2Xgr+WpuS3juOABbBCvbqSvpym4WzP/SDMuvGz6qH+EFgm19l8SUHUGA==",
                EmailConfirmed = true,
                DateCreated = DateTime.SpecifyKind(new DateTime(2025, 11, 10), DateTimeKind.Utc),
            };

            var userRole = new UserRole
            {
                Id = new Guid("7ad9b1e1-4c23-46a2-b8e4-219ab417f71f"),
                RoleId = adminRoleId,
                UserId = adminUserId,
                DateCreated = DateTime.SpecifyKind(new DateTime(2025, 11, 10), DateTimeKind.Utc)
            };

            var adminProfile = new Admin
            {
                Id = new Guid("f0e25b73-7d1a-4c19-8b2f-09a3efb40d12"),
                FirstName = "Admin",
                LastName = "Hms",
                Address = "Lagos State",
                Gender = Models.Enums.Gender.Male,
                PhoneNumber = "+2349020880996",
                UserId = adminUserId,
                DateOfBirth = DateTime.SpecifyKind(new DateTime(1997, 11, 10), DateTimeKind.Utc),
                DateCreated = DateTime.SpecifyKind(new DateTime(2025, 11, 10), DateTimeKind.Utc),
            };

            modelBuilder.Entity<Role>().HasData(role);
            modelBuilder.Entity<Admin>().HasData(adminProfile);
            modelBuilder.Entity<User>().HasData(adminUser);
            modelBuilder.Entity<UserRole>().HasData(userRole);
        }

        private void SeedRoleData(ModelBuilder modelBuilder)
        {
            var studentRole = new Role
            {
                Id = new Guid("56789c4e-92e2-41a7-92c2-4292f0df5f26"),
                Name = "Student",
                Description = "Can Check Admission Status and other things",
                DateCreated = new DateTime(2026, 3, 9, 13, 6, 50, 158, DateTimeKind.Utc).AddTicks(8309),
            };

            modelBuilder.Entity<Role>().HasData(studentRole);
        }

        DbSet<User> Users => Set<User>();
        DbSet<UserRole> UserRoles => Set<UserRole>();
        DbSet<Student> Students => Set<Student>();
        DbSet<Role> Roles => Set<Role>();
        DbSet<Department> Departments => Set<Department>();
        DbSet<Passport> Documents => Set<Passport>();
        DbSet<Waec> Waecs => Set<Waec>();
        DbSet<Payment> Payments => Set<Payment>();
        DbSet<RegistrationDraft> RegistrationDrafts => Set<RegistrationDraft>();
        DbSet<Admin> Admins => Set<Admin>();
    }
}
