using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalOnboardingApplication.Migrations
{
    /// <inheritdoc />
    public partial class SeesionTestAvgDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageDifficulty",
                table: "TestSessions",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageDifficulty",
                table: "TestSessions");
        }
    }
}
