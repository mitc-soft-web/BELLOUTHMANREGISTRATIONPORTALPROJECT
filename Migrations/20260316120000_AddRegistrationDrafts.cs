using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Mitc_Registration_Portal.Persistence.Context;

#nullable disable

namespace Mitc_Registration_Portal.Migrations
{
    [DbContext(typeof(StudentDbContext))]
    [Migration("20260316120000_AddRegistrationDrafts")]
    public partial class AddRegistrationDrafts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                    name: "RegistrationDrafts",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                        Email = table.Column<string>(type: "varchar(255)", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        FirstName = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        LastName = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        MiddleName = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Gender = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                        PhoneNumber = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Address = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        City = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        State = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        NIN = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        DepartmentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                        PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ExamNo = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ExamYear = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CertificatesPath = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        PassportPath = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CurrentPaymentReference = table.Column<string>(type: "varchar(100)", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Status = table.Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        DateUpdatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        ExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_RegistrationDrafts", x => x.Id);
                    })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDrafts_Email",
                table: "RegistrationDrafts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationDrafts_CurrentPaymentReference",
                table: "RegistrationDrafts",
                column: "CurrentPaymentReference",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationDrafts");
        }
    }
}

