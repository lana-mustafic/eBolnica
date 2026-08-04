using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationImageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "MedicationImages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "MedicationImages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "MedicationImages",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "MedicationImages");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MedicationImages");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "MedicationImages");
        }
    }
}
