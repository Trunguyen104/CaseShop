using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseShop.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPreparedSupportChatbot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatbotKnowledgeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Keywords = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    ActionLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotKnowledgeEntries", x => x.Id);
                    table.CheckConstraint("CK_ChatbotKnowledgeEntries_Action", "(\"ActionType\" = 0 AND \"ActionUrl\" IS NULL) OR (\"ActionType\" = 1 AND \"ActionUrl\" IS NOT NULL)");
                    table.CheckConstraint("CK_ChatbotKnowledgeEntries_Priority", "\"Priority\" >= 0 AND \"Priority\" <= 1000");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotKnowledgeEntries_IntentCode",
                table: "ChatbotKnowledgeEntries",
                column: "IntentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotKnowledgeEntries_IsActive_Category_Priority",
                table: "ChatbotKnowledgeEntries",
                columns: new[] { "IsActive", "Category", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatbotKnowledgeEntries");
        }
    }
}
