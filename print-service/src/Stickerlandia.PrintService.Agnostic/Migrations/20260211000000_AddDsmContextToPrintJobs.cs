using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable

namespace Stickerlandia.PrintService.Agnostic.Migrations
{
    /// <inheritdoc />
    public partial class AddDsmContextToPrintJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                table: "PrintJobs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PropagationHeadersJson",
                table: "PrintJobs",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceParent",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "PropagationHeadersJson",
                table: "PrintJobs");
        }
    }
}
