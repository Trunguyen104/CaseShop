using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseShop.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusEmailTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastNotifiedStatus",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusEmailAttempts",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StatusEmailLastError",
                table: "Orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusEmailSentAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusEmailStatus",
                table: "Orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastNotifiedStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StatusEmailAttempts",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StatusEmailLastError",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StatusEmailSentAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StatusEmailStatus",
                table: "Orders");
        }
    }
}
