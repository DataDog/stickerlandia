using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable
#nullable disable

namespace Stickerlandia.UserManagement.Agnostic.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrintedStickerCount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintedStickerCount",
                table: "users");
        }
    }
}
