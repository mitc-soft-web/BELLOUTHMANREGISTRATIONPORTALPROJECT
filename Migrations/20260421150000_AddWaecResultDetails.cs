using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Mitc_Registration_Portal.Persistence.Context;

#nullable disable

namespace Mitc_Registration_Portal.Migrations
{
    [DbContext(typeof(StudentDbContext))]
    [Migration("20260421150000_AddWaecResultDetails")]
    public partial class AddWaecResultDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                    name: "WaecResultDetailsJson",
                    table: "RegistrationDrafts",
                    type: "longtext",
                    nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                    name: "ResultDetailsJson",
                    table: "Waecs",
                    type: "longtext",
                    nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaecResultDetailsJson",
                table: "RegistrationDrafts");

            migrationBuilder.DropColumn(
                name: "ResultDetailsJson",
                table: "Waecs");
        }
    }
}
