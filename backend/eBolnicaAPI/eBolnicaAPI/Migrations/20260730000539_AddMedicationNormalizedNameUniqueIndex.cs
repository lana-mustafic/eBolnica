using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationNormalizedNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Medications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE Medications SET NormalizedName = LOWER(LTRIM(RTRIM(Name)))");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Medications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 1,
                column: "NormalizedName",
                value: "paracetamol");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 2,
                column: "NormalizedName",
                value: "ibuprofen");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 3,
                column: "NormalizedName",
                value: "amoxicillin");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 4,
                column: "NormalizedName",
                value: "aspirin");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 5,
                column: "NormalizedName",
                value: "cetirizine");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 6,
                column: "NormalizedName",
                value: "omeprazole");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 7,
                column: "NormalizedName",
                value: "metformin");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 8,
                column: "NormalizedName",
                value: "loratadine");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 9,
                column: "NormalizedName",
                value: "azithromycin");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 10,
                column: "NormalizedName",
                value: "vitamin d3");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 11,
                column: "NormalizedName",
                value: "ciprofloxacin");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 12,
                column: "NormalizedName",
                value: "diclofenac");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 13,
                column: "NormalizedName",
                value: "fexofenadine");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 14,
                column: "NormalizedName",
                value: "calcium carbonate");

            migrationBuilder.UpdateData(
                table: "Medications",
                keyColumn: "Id",
                keyValue: 15,
                column: "NormalizedName",
                value: "atorvastatin");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_NormalizedName_Unique",
                table: "Medications",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medications_NormalizedName_Unique",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Medications");
        }
    }
}
