using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseShop.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseTemplateFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneBrands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneBrands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneBrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneModels_PhoneBrands_PhoneBrandId",
                        column: x => x.PhoneBrandId,
                        principalTable: "PhoneBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CanvasWidth = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    CanvasHeight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    CornerRadius = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    SafeAreaX = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    SafeAreaY = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    SafeAreaWidth = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    SafeAreaHeight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PrintAreaX = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PrintAreaY = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PrintAreaWidth = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PrintAreaHeight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    BleedTop = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    BleedRight = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    BleedBottom = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    BleedLeft = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    MaskImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OverlayImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                        name: "FK_CaseTemplates_CaseTypes_CaseTypeId",
                        column: x => x.CaseTypeId,
                        principalTable: "CaseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseTemplates_PhoneModels_PhoneModelId",
                        column: x => x.PhoneModelId,
                        principalTable: "PhoneModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseTemplates_CaseTypeId",
                table: "CaseTemplates",
                column: "CaseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTemplates_PhoneModelId_CaseTypeId_Slug",
                table: "CaseTemplates",
                columns: new[] { "PhoneModelId", "CaseTypeId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseTypes_Slug",
                table: "CaseTypes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneBrands_Slug",
                table: "PhoneBrands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneModels_PhoneBrandId_Slug",
                table: "PhoneModels",
                columns: new[] { "PhoneBrandId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseTemplates");

            migrationBuilder.DropTable(
                name: "CaseTypes");

            migrationBuilder.DropTable(
                name: "PhoneModels");

            migrationBuilder.DropTable(
                name: "PhoneBrands");
        }
    }
}
