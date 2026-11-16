using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KemisOnboarding.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQasoCertificateChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CertificateAuthenticityVerified",
                table: "QasoInspections",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CertificateComplete",
                table: "QasoInspections",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CertificateIssuerVerified",
                table: "QasoInspections",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CertificateNotExpired",
                table: "QasoInspections",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateAuthenticityVerified",
                table: "QasoInspections");

            migrationBuilder.DropColumn(
                name: "CertificateComplete",
                table: "QasoInspections");

            migrationBuilder.DropColumn(
                name: "CertificateIssuerVerified",
                table: "QasoInspections");

            migrationBuilder.DropColumn(
                name: "CertificateNotExpired",
                table: "QasoInspections");
        }
    }
}
