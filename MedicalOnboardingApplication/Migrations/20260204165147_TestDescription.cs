using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalOnboardingApplication.Migrations
{
    /// <inheritdoc />
    public partial class TestDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tests");
        }
    }
}
