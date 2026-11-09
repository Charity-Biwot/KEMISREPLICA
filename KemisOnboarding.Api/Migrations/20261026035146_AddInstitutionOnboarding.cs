using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KemisOnboarding.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Institutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResumeCodeHash = table.Column<string>(type: "text", nullable: false),
                    StateDepartment = table.Column<int>(type: "integer", nullable: false),
                    InstitutionType = table.Column<string>(type: "text", nullable: false),
                    ApplicationType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NormalizedName = table.Column<string>(type: "text", nullable: false),
                    NameAsOnCertificate = table.Column<string>(type: "text", nullable: true),
                    Uic = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    MobileNumber = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    PostalLocation = table.Column<string>(type: "text", nullable: true),
                    County = table.Column<string>(type: "text", nullable: false),
                    SubCounty = table.Column<string>(type: "text", nullable: false),
                    Constituency = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    SchoolCategories = table.Column<string>(type: "text", nullable: true),
                    AccommodationType = table.Column<int>(type: "integer", nullable: false),
                    HasCertificate = table.Column<bool>(type: "boolean", nullable: false),
                    CertificateNumber = table.Column<string>(type: "text", nullable: true),
                    NormalizedCertificateNumber = table.Column<string>(type: "text", nullable: true),
                    CertificateFilePath = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_NormalizedCertificateNumber",
                table: "Institutions",
                column: "NormalizedCertificateNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_NormalizedName_Level_SubCounty",
                table: "Institutions",
                columns: new[] { "NormalizedName", "Level", "SubCounty" });

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Uic",
                table: "Institutions",
                column: "Uic",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Institutions");
        }
    }
}
