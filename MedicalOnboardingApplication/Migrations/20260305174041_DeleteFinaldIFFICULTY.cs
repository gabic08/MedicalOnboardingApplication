using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalOnboardingApplication.Migrations
{
    /// <inheritdoc />
    public partial class DeleteFinaldIFFICULTY : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalDifficulty",
                table: "TestSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinalDifficulty",
                table: "TestSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
