using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stickerlandia.UserManagement.Agnostic.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    email_address = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    event_data = table.Column<string>(type: "text", nullable: false),
                    event_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false),
                    failed = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    trace_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    email_address = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    claimed_sticker_count = table.Column<int>(type: "integer", nullable: false),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    account_tier = table.Column<int>(type: "integer", nullable: false),
                    account_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_email_address",
                table: "users",
                column: "email_address",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_items");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
