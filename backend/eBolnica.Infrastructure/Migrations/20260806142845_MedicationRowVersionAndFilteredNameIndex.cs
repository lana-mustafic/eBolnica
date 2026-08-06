using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MedicationRowVersionAndFilteredNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medications_NormalizedName",
                table: "Medications");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Medications",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Medications_NormalizedName",
                table: "Medications",
                column: "NormalizedName",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medications_NormalizedName",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Medications");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_NormalizedName",
                table: "Medications",
                column: "NormalizedName",
                unique: true);
        }
    }
}
