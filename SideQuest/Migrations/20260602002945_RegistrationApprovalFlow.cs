using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SideQuest.Migrations
{
    /// <inheritdoc />
    public partial class RegistrationApprovalFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "WorkerProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "WorkerProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "WorkerProfiles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidenceCity",
                table: "WorkerProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidenceCountry",
                table: "WorkerProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationDateOfBirth",
                table: "WorkerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationDocumentPath",
                table: "WorkerProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "WorkerProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationRejectionMessage",
                table: "WorkerProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationRejectionReason",
                table: "WorkerProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationReviewedAt",
                table: "WorkerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationReviewedByAdminId",
                table: "WorkerProfiles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "WorkerProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationSubmittedAt",
                table: "WorkerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "CompanyProfiles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizedRepresentativeName",
                table: "CompanyProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizedRepresentativeNationalId",
                table: "CompanyProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalCompanyName",
                table: "CompanyProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "CompanyProfiles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "CompanyProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "CompanyProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationDocumentPath",
                table: "CompanyProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "CompanyProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationRejectionMessage",
                table: "CompanyProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationRejectionReason",
                table: "CompanyProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationReviewedAt",
                table: "CompanyProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationReviewedByAdminId",
                table: "CompanyProfiles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "CompanyProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationSubmittedAt",
                table: "CompanyProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProfiles_VerificationStatus",
                table: "WorkerProfiles",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_VerificationStatus",
                table: "CompanyProfiles",
                column: "VerificationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkerProfiles_VerificationStatus",
                table: "WorkerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_VerificationStatus",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "ResidenceCity",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "ResidenceCountry",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationDateOfBirth",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationDocumentPath",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationRejectionMessage",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationRejectionReason",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedAt",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedByAdminId",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationSubmittedAt",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "AuthorizedRepresentativeName",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "AuthorizedRepresentativeNationalId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "LegalCompanyName",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationDocumentPath",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationRejectionMessage",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationRejectionReason",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedAt",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedByAdminId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationSubmittedAt",
                table: "CompanyProfiles");
        }
    }
}
