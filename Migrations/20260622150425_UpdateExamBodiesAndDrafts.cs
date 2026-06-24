using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mitc_Registration_Portal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExamBodiesAndDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExaminationBody",
                table: "Waecs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SecondExaminationBody",
                table: "Waecs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExamBody",
                table: "RegistrationDrafts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SecondExamBody",
                table: "RegistrationDrafts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExaminationBody",
                table: "Waecs");

            migrationBuilder.DropColumn(
                name: "SecondExaminationBody",
                table: "Waecs");

            migrationBuilder.DropColumn(
                name: "ExamBody",
                table: "RegistrationDrafts");

            migrationBuilder.DropColumn(
                name: "SecondExamBody",
                table: "RegistrationDrafts");
        }
    }
}
