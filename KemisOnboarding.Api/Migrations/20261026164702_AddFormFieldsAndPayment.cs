using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KemisOnboarding.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFormFieldsAndPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApplicationFee",
                table: "Institutions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalStartedAt",
                table: "Institutions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstitutionLevelName",
                table: "Institutions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Institutions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Institutions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationFee",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "ApprovalStartedAt",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "InstitutionLevelName",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Institutions");
        }
    }
}
