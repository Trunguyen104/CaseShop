using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseShop.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPhoneModelSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseTemplates");

            migrationBuilder.AddColumn<decimal>(
                name: "BleedBottom",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BleedLeft",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BleedRight",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BleedTop",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CanvasHeight",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CanvasWidth",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CornerRadius",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MaskImageUrl",
                table: "PhoneModels",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverlayImageUrl",
                table: "PhoneModels",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrintAreaHeight",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrintAreaWidth",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrintAreaX",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrintAreaY",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SafeAreaHeight",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SafeAreaWidth",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SafeAreaX",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SafeAreaY",
                table: "PhoneModels",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_Bleed",
                table: "PhoneModels",
                sql: "\"BleedTop\" >= 0 AND \"BleedRight\" >= 0 AND \"BleedBottom\" >= 0 AND \"BleedLeft\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_Canvas",
                table: "PhoneModels",
                sql: "\"CanvasWidth\" > 0 AND \"CanvasHeight\" > 0 AND \"CornerRadius\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_PrintArea",
                table: "PhoneModels",
                sql: "\"PrintAreaX\" >= 0 AND \"PrintAreaY\" >= 0 AND \"PrintAreaWidth\" > 0 AND \"PrintAreaHeight\" > 0 AND \"PrintAreaX\" + \"PrintAreaWidth\" <= \"CanvasWidth\" AND \"PrintAreaY\" + \"PrintAreaHeight\" <= \"CanvasHeight\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_SafeArea",
                table: "PhoneModels",
                sql: "\"SafeAreaX\" >= 0 AND \"SafeAreaY\" >= 0 AND \"SafeAreaWidth\" > 0 AND \"SafeAreaHeight\" > 0 AND \"SafeAreaX\" + \"SafeAreaWidth\" <= \"CanvasWidth\" AND \"SafeAreaY\" + \"SafeAreaHeight\" <= \"CanvasHeight\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_Bleed",
                table: "PhoneModels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_Canvas",
                table: "PhoneModels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_PrintArea",
                table: "PhoneModels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_SafeArea",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "BleedBottom",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "BleedLeft",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "BleedRight",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "BleedTop",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "CanvasHeight",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "CanvasWidth",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "CornerRadius",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "MaskImageUrl",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "OverlayImageUrl",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "PrintAreaHeight",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "PrintAreaWidth",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "PrintAreaX",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "PrintAreaY",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "SafeAreaHeight",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "SafeAreaWidth",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "SafeAreaX",
                table: "PhoneModels");

            migrationBuilder.DropColumn(
                name: "SafeAreaY",
                table: "PhoneModels");

            migrationBuilder.CreateTable(
                name: "CaseTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    BleedBottom = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    BleedLeft = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    BleedRight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    BleedTop = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    CanvasHeight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    CanvasWidth = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    CornerRadius = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MaskImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OverlayImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PrintAreaHeight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PrintAreaWidth = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PrintAreaX = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PrintAreaY = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    SafeAreaHeight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    SafeAreaWidth = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    SafeAreaX = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    SafeAreaY = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTemplates", x => x.Id);
                    table.CheckConstraint("CK_CaseTemplates_Bleed", "\"BleedTop\" >= 0 AND \"BleedRight\" >= 0 AND \"BleedBottom\" >= 0 AND \"BleedLeft\" >= 0");
                    table.CheckConstraint("CK_CaseTemplates_Canvas", "\"CanvasWidth\" > 0 AND \"CanvasHeight\" > 0 AND \"CornerRadius\" >= 0");
                    table.CheckConstraint("CK_CaseTemplates_PrintArea", "\"PrintAreaX\" >= 0 AND \"PrintAreaY\" >= 0 AND \"PrintAreaWidth\" > 0 AND \"PrintAreaHeight\" > 0 AND \"PrintAreaX\" + \"PrintAreaWidth\" <= \"CanvasWidth\" AND \"PrintAreaY\" + \"PrintAreaHeight\" <= \"CanvasHeight\"");
                    table.CheckConstraint("CK_CaseTemplates_SafeArea", "\"SafeAreaX\" >= 0 AND \"SafeAreaY\" >= 0 AND \"SafeAreaWidth\" > 0 AND \"SafeAreaHeight\" > 0 AND \"SafeAreaX\" + \"SafeAreaWidth\" <= \"CanvasWidth\" AND \"SafeAreaY\" + \"SafeAreaHeight\" <= \"CanvasHeight\"");
                    table.ForeignKey(
                        name: "FK_CaseTemplates_PhoneModels_PhoneModelId",
                        column: x => x.PhoneModelId,
                        principalTable: "PhoneModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseTemplates_PhoneModelId_Slug",
                table: "CaseTemplates",
                columns: new[] { "PhoneModelId", "Slug" },
                unique: true);
        }
    }
}
