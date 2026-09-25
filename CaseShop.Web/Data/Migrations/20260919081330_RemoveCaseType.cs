using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseShop.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCaseType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseTemplates_CaseTypes_CaseTypeId",
                table: "CaseTemplates");

            migrationBuilder.DropTable(
                name: "CaseTypes");

            migrationBuilder.DropIndex(
                name: "IX_CaseTemplates_CaseTypeId",
                table: "CaseTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CaseTemplates_PhoneModelId_CaseTypeId_Slug",
                table: "CaseTemplates");

            migrationBuilder.DropColumn(
                name: "CaseTypeId",
                table: "CaseTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTemplates_PhoneModelId_Slug",
                table: "CaseTemplates",
                columns: new[] { "PhoneModelId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CaseTemplates_PhoneModelId_Slug",
                table: "CaseTemplates");

            migrationBuilder.AddColumn<Guid>(
                name: "CaseTypeId",
                table: "CaseTemplates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CaseTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTypes", x => x.Id);
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

            migrationBuilder.AddForeignKey(
                name: "FK_CaseTemplates_CaseTypes_CaseTypeId",
                table: "CaseTemplates",
                column: "CaseTypeId",
                principalTable: "CaseTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
