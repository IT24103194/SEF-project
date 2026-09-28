using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGym.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityResolutionEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "issue_images",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "issue_images",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "issue_images",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                table: "facility_issues",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationStatus",
                table: "facility_issues",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "facility_issues",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SanitizedDescription",
                table: "facility_issues",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "issue_images");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "issue_images");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "issue_images");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                table: "facility_issues");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "facility_issues");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "facility_issues");

            migrationBuilder.DropColumn(
                name: "SanitizedDescription",
                table: "facility_issues");
        }
    }
}
