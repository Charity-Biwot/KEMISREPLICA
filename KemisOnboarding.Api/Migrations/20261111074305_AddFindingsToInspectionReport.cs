using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KemisOnboarding.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFindingsToInspectionReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Findings",
                table: "InspectionReports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Findings",
                table: "InspectionReports");
        }
    }
}
