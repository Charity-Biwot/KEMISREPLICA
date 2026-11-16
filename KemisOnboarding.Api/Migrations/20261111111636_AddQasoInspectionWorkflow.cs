using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KemisOnboarding.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQasoInspectionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QasoInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateStatus = table.Column<int>(type: "integer", nullable: false),
                    CertificateReviewNotes = table.Column<string>(type: "text", nullable: true),
                    CertificateReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhysicalInspectionStatus = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduleNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QasoInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QasoInspections_InstitutionAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "InstitutionAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QasoInspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    StoredFileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionEvidence_QasoInspections_QasoInspectionId",
                        column: x => x.QasoInspectionId,
                        principalTable: "QasoInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionEvidence_QasoInspectionId",
                table: "InspectionEvidence",
                column: "QasoInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_QasoInspections_AssignmentId",
                table: "QasoInspections",
                column: "AssignmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionEvidence");

            migrationBuilder.DropTable(
                name: "QasoInspections");
        }
    }
}
