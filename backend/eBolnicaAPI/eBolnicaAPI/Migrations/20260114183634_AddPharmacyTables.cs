using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    MinimumStockLevel = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequiresPrescription = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DosageForm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pharmacists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pharmacists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pharmacists_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MedicalReportId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    PharmacistId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrescribedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DispensedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prescriptions_MedicalReports_MedicalReportId",
                        column: x => x.MedicalReportId,
                        principalTable: "MedicalReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Pharmacists_PharmacistId",
                        column: x => x.PharmacistId,
                        principalTable: "Pharmacists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionId = table.Column<int>(type: "int", nullable: false),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "FirstName", "LastName", "LicenseNumber", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName", "UserType" },
                values: new object[] { "ph1", 0, "fixed-guid-1", "pharmacist@pharmacy.com", false, "Milan", "Jovanović", "PH-L1", true, null, "PHARMACIST@PHARMACY.COM", "PHARMACIST@PHARMACY.COM", null, "061200200", false, "fixed-guid-2", false, "pharmacist@pharmacy.com", "Pharmacist" });

            migrationBuilder.InsertData(
                table: "Medications",
                columns: new[] { "Id", "BatchNumber", "Category", "CreatedAt", "Description", "DosageForm", "ExpiryDate", "GenericName", "IsActive", "Manufacturer", "MinimumStockLevel", "Name", "Price", "RequiresPrescription", "StockQuantity", "Strength", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "BATCH-001", "Painkillers", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pain reliever and fever reducer", "Tablet", new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "Acetaminophen", true, "PharmaCorp", 100, "Paracetamol", 250.00m, false, 500, "500mg", null },
                    { 2, "BATCH-002", "Painkillers", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nonsteroidal anti-inflammatory drug", "Tablet", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ibuprofen", true, "MediPharm", 80, "Ibuprofen", 320.00m, false, 350, "400mg", null },
                    { 3, "BATCH-003", "Antibiotics", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Antibiotic for bacterial infections", "Capsule", new DateTime(2025, 9, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Amoxicillin", true, "AntibioPharm", 50, "Amoxicillin", 850.00m, true, 200, "500mg", null },
                    { 4, "BATCH-004", "Painkillers", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pain reliever, anti-inflammatory, and blood thinner", "Tablet", new DateTime(2027, 3, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Acetylsalicylic acid", true, "PharmaCorp", 50, "Aspirin", 180.00m, false, 45, "100mg", null },
                    { 5, "BATCH-005", "Antihistamines", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Antihistamine for allergies", "Tablet", new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cetirizine", true, "AllergyMed", 60, "Cetirizine", 420.00m, false, 280, "10mg", null },
                    { 6, "BATCH-006", "Gastrointestinal", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Proton pump inhibitor for acid reflux", "Capsule", new DateTime(2025, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Omeprazole", true, "DigestPharm", 40, "Omeprazole", 650.00m, true, 150, "20mg", null },
                    { 7, "BATCH-007", "Antidiabetic", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Antidiabetic medication", "Tablet", new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Metformin", true, "DiabetPharm", 30, "Metformin", 550.00m, true, 120, "500mg", null },
                    { 8, "BATCH-008", "Antihistamines", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Antihistamine for seasonal allergies", "Tablet", new DateTime(2026, 7, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Loratadine", true, "AllergyMed", 70, "Loratadine", 380.00m, false, 320, "10mg", null },
                    { 9, "BATCH-009", "Antibiotics", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Broad-spectrum antibiotic", "Tablet", new DateTime(2025, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Azithromycin", true, "AntibioPharm", 25, "Azithromycin", 1200.00m, true, 80, "500mg", null },
                    { 10, "BATCH-010", "Vitamins", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Vitamin D supplement", "Capsule", new DateTime(2027, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cholecalciferol", true, "VitaminsPlus", 150, "Vitamin D3", 450.00m, false, 600, "2000 IU", null },
                    { 11, "BATCH-011", "Antibiotics", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fluoroquinolone antibiotic", "Tablet", new DateTime(2025, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ciprofloxacin", true, "AntibioPharm", 20, "Ciprofloxacin", 950.00m, true, 35, "500mg", null },
                    { 12, "BATCH-012", "Painkillers", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "NSAID for pain and inflammation", "Tablet", new DateTime(2026, 5, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Diclofenac", true, "MediPharm", 45, "Diclofenac", 520.00m, true, 180, "50mg", null },
                    { 13, "BATCH-013", "Antihistamines", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Antihistamine for allergies", "Tablet", new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fexofenadine", true, "AllergyMed", 55, "Fexofenadine", 480.00m, false, 250, "120mg", null },
                    { 14, "BATCH-014", "Supplements", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Calcium supplement and antacid", "Tablet", new DateTime(2027, 2, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Calcium Carbonate", true, "VitaminsPlus", 100, "Calcium Carbonate", 280.00m, false, 400, "500mg", null },
                    { 15, "BATCH-015", "Cardiovascular", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Cholesterol-lowering medication", "Tablet", new DateTime(2026, 11, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "Atorvastatin", true, "CardioPharm", 25, "Atorvastatin", 1100.00m, true, 95, "20mg", null }
                });

            migrationBuilder.InsertData(
                table: "Pharmacists",
                columns: new[] { "Id", "Address", "AppUserId", "CreatedAt", "FirstName", "HireDate", "LastName", "LicenseNumber", "PhoneNumber", "UpdatedAt" },
                values: new object[] { 1, "Apotekarska 15, Beograd", "ph1", new DateTime(2020, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Milan", new DateTime(2020, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Jovanović", "PH-L1", "061200200", null });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_Name",
                table: "Medications",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacists_AppUserId",
                table: "Pharmacists",
                column: "AppUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacists_LicenseNumber",
                table: "Pharmacists",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_MedicationId",
                table: "PrescriptionItems",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId",
                table: "Prescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_MedicalReportId",
                table: "Prescriptions",
                column: "MedicalReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PharmacistId",
                table: "Prescriptions",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescriptionNumber",
                table: "Prescriptions",
                column: "PrescriptionNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "Pharmacists");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "ph1");
        }
    }
}
