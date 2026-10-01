using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseShop.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeliveryMapLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_DeliveryCoordinates",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsLocationConfirmed",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocationConfirmed",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Orders",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Orders",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_DeliveryCoordinates",
                table: "Orders",
                sql: "((\"Latitude\" IS NULL AND \"Longitude\" IS NULL) OR (\"Latitude\" BETWEEN 8 AND 24 AND \"Longitude\" BETWEEN 102 AND 111)) AND (NOT \"IsLocationConfirmed\" OR (\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL))");
        }
    }
}
