using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseShop.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraCutoutToPhoneModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CameraCutoutHeight",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CameraCutoutRadius",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CameraCutoutWidth",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CameraCutoutX",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CameraCutoutY",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_CameraCutout",
                table: "PhoneModels",
                sql: "(\"CameraCutoutWidth\" = 0 AND \"CameraCutoutHeight\" = 0) OR (\"CameraCutoutX\" >= 0 AND \"CameraCutoutY\" >= 0 AND \"CameraCutoutWidth\" > 0 AND \"CameraCutoutHeight\" > 0 AND \"CameraCutoutRadius\" >= 0 AND \"CameraCutoutX\" + \"CameraCutoutWidth\" <= \"CanvasWidth\" AND \"CameraCutoutY\" + \"CameraCutoutHeight\" <= \"CanvasHeight\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_CameraCutout",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "CameraCutoutHeight",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "CameraCutoutRadius",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "CameraCutoutWidth",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "CameraCutoutX",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "CameraCutoutY",
                table: "PhoneModels");
        }
    }
}
