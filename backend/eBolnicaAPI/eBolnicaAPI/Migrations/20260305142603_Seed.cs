using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class Seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Medications",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1,
                column: "Gender",
                value: "Male");

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2,
                column: "Gender",
                value: "Male");

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 3,
                column: "Gender",
                value: "Male");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId_Status",
                table: "Prescriptions",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescribedDate",
                table: "Prescriptions",
                column: "PrescribedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_Status_CreatedAt",
                table: "Prescriptions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_Status_DispensedDate",
                table: "Prescriptions",
                columns: new[] { "Status", "DispensedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId_MedicationId",
                table: "PrescriptionItems",
                columns: new[] { "PrescriptionId", "MedicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Category",
                table: "Medications",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Category_IsActive",
                table: "Medications",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_ExpiryDate",
                table: "Medications",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_IsActive_CreatedAt",
                table: "Medications",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Name_Category",
                table: "Medications",
                columns: new[] { "Name", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Price_StockQuantity",
                table: "Medications",
                columns: new[] { "Price", "StockQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_StockQuantity",
                table: "Medications",
                column: "StockQuantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_PatientId_Status",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_PrescribedDate",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_Status_CreatedAt",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_Status_DispensedDate",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_PrescriptionId_MedicationId",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_Medications_Category",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_Category_IsActive",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_ExpiryDate",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_IsActive_CreatedAt",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_Name_Category",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_Price_StockQuantity",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_StockQuantity",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Doctors");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Medications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");
        }
    }
}
