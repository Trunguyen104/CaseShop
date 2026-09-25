using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseShop.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RelaxSafeAndPrintAreaConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_PrintArea",
                table: "PhoneModels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_SafeArea",
                table: "PhoneModels");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_PrintArea",
                table: "PhoneModels",
                sql: "(\"PrintAreaWidth\" = 0 AND \"PrintAreaHeight\" = 0) OR (\"PrintAreaX\" >= 0 AND \"PrintAreaY\" >= 0 AND \"PrintAreaWidth\" > 0 AND \"PrintAreaHeight\" > 0 AND \"PrintAreaX\" + \"PrintAreaWidth\" <= \"CanvasWidth\" AND \"PrintAreaY\" + \"PrintAreaHeight\" <= \"CanvasHeight\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_SafeArea",
                table: "PhoneModels",
                sql: "(\"SafeAreaWidth\" = 0 AND \"SafeAreaHeight\" = 0) OR (\"SafeAreaX\" >= 0 AND \"SafeAreaY\" >= 0 AND \"SafeAreaWidth\" > 0 AND \"SafeAreaHeight\" > 0 AND \"SafeAreaX\" + \"SafeAreaWidth\" <= \"CanvasWidth\" AND \"SafeAreaY\" + \"SafeAreaHeight\" <= \"CanvasHeight\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_PrintArea",
                table: "PhoneModels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneModels_SafeArea",
                table: "PhoneModels");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_PrintArea",
                table: "PhoneModels",
                sql: "\"PrintAreaX\" >= 0 AND \"PrintAreaY\" >= 0 AND \"PrintAreaWidth\" > 0 AND \"PrintAreaHeight\" > 0 AND \"PrintAreaX\" + \"PrintAreaWidth\" <= \"CanvasWidth\" AND \"PrintAreaY\" + \"PrintAreaHeight\" <= \"CanvasHeight\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneModels_SafeArea",
                table: "PhoneModels",
                sql: "\"SafeAreaX\" >= 0 AND \"SafeAreaY\" >= 0 AND \"SafeAreaWidth\" > 0 AND \"SafeAreaHeight\" > 0 AND \"SafeAreaX\" + \"SafeAreaWidth\" <= \"CanvasWidth\" AND \"SafeAreaY\" + \"SafeAreaHeight\" <= \"CanvasHeight\"");
        }
    }
}
