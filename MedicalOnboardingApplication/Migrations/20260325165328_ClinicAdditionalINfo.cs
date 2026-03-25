using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalOnboardingApplication.Migrations
{
    /// <inheritdoc />
    public partial class ClinicAdditionalINfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Clinics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Clinics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Clinics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Clinics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Clinics",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Clinics");
        }
    }
}
