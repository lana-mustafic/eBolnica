using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

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
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RegistrationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
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
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BloodType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAdmitted = table.Column<bool>(type: "bit", nullable: true),
                    RegistrationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Files_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    RecordNumber = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicalRecordId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Symptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Therapy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalReports_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalReports_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                values: new object[,]
                {
                    { "a1", 0, "a0b1c2d3-e4f5-4001-8001-000000000001", "admin@gmail.com", true, "Admin", "User", null, true, null, "ADMIN@GMAIL.COM", "ADMIN@GMAIL.COM", null, "061000000", true, "a0b1c2d3-e4f5-4001-8002-000000000001", false, "admin@gmail.com", "Admin" },
                    { "d1", 0, "a0b1c2d3-e4f5-4002-8001-000000000002", "marko@gmail.com", false, "Marko", "Marković", "L1", true, null, "MARKO@GMAIL.COM", "MARKO@GMAIL.COM", null, "061100100", false, "a0b1c2d3-e4f5-4002-8002-000000000002", false, "marko@gmail.com", "Doctor" },
                    { "d2", 0, "a0b1c2d3-e4f5-4003-8001-000000000003", "senad@gmail.com", false, "Senad", "Husić", "L2", true, null, "SENAD@GMAIL.COM", "SENAD@GMAIL.COM", null, "061100101", false, "a0b1c2d3-e4f5-4003-8002-000000000003", false, "senad@gmail.com", "Doctor" },
                    { "d3", 0, "a0b1c2d3-e4f5-4004-8001-000000000004", "petar@gmail.com", false, "Petar", "Petrović", "L3", true, null, "PETAR@GMAIL.COM", "PETAR@GMAIL.COM", null, "061100102", false, "a0b1c2d3-e4f5-4004-8002-000000000004", false, "petar@gmail.com", "Doctor" },
                    { "p1", 0, "a0b1c2d3-e4f5-4005-8001-000000000005", "ismet@gmail.com", false, "Ismet", "Horo", null, true, null, "ISMET@GMAIL.COM", "ISMET@GMAIL.COM", null, "061100103", false, "a0b1c2d3-e4f5-4005-8002-000000000005", false, "ismet@gmail.com", "Patient" },
                    { "p10", 0, "a0b1c2d3-e4f5-4014-8001-000000000014", "milica@gmail.com", false, "Milica", "Radić", null, true, null, "MILICA@GMAIL.COM", "MILICA@GMAIL.COM", null, "061100112", false, "a0b1c2d3-e4f5-4014-8002-000000000014", false, "milica@gmail.com", "Patient" },
                    { "p11", 0, "a0b1c2d3-e4f5-4015-8001-000000000015", "luka@gmail.com", false, "Luka", "Stefanović", null, true, null, "LUKA@GMAIL.COM", "LUKA@GMAIL.COM", null, "061100113", false, "a0b1c2d3-e4f5-4015-8002-000000000015", false, "luka@gmail.com", "Patient" },
                    { "p12", 0, "a0b1c2d3-e4f5-4016-8001-000000000016", "teodora@gmail.com", false, "Teodora", "Lazić", null, true, null, "TEODORA@GMAIL.COM", "TEODORA@GMAIL.COM", null, "061100114", false, "a0b1c2d3-e4f5-4016-8002-000000000016", false, "teodora@gmail.com", "Patient" },
                    { "p2", 0, "a0b1c2d3-e4f5-4006-8001-000000000006", "elon@gmail.com", false, "Elon", "Musk", null, true, null, "ELON@GMAIL.COM", "ELON@GMAIL.COM", null, "061100104", false, "a0b1c2d3-e4f5-4006-8002-000000000006", false, "elon@gmail.com", "Patient" },
                    { "p3", 0, "a0b1c2d3-e4f5-4007-8001-000000000007", "peter@gmail.com", false, "Peter", "Griffin", null, true, null, "PETER@GMAIL.COM", "PETER@GMAIL.COM", null, "061100105", false, "a0b1c2d3-e4f5-4007-8002-000000000007", false, "peter@gmail.com", "Patient" },
                    { "p4", 0, "a0b1c2d3-e4f5-4008-8001-000000000008", "ana@gmail.com", false, "Ana", "Jovanović", null, true, null, "ANA@GMAIL.COM", "ANA@GMAIL.COM", null, "061100106", false, "a0b1c2d3-e4f5-4008-8002-000000000008", false, "ana@gmail.com", "Patient" },
                    { "p5", 0, "a0b1c2d3-e4f5-4009-8001-000000000009", "marko.patient@gmail.com", false, "Marko", "Nikolić", null, true, null, "MARKO.PATIENT@GMAIL.COM", "MARKO.PATIENT@GMAIL.COM", null, "061100107", false, "a0b1c2d3-e4f5-4009-8002-000000000009", false, "marko.patient@gmail.com", "Patient" },
                    { "p6", 0, "a0b1c2d3-e4f5-4010-8001-000000000010", "sara@gmail.com", false, "Sara", "Stojanović", null, true, null, "SARA@GMAIL.COM", "SARA@GMAIL.COM", null, "061100108", false, "a0b1c2d3-e4f5-4010-8002-000000000010", false, "sara@gmail.com", "Patient" },
                    { "p7", 0, "a0b1c2d3-e4f5-4011-8001-000000000011", "nikola@gmail.com", false, "Nikola", "Popović", null, true, null, "NIKOLA@GMAIL.COM", "NIKOLA@GMAIL.COM", null, "061100109", false, "a0b1c2d3-e4f5-4011-8002-000000000011", false, "nikola@gmail.com", "Patient" },
                    { "p8", 0, "a0b1c2d3-e4f5-4012-8001-000000000012", "jovana@gmail.com", false, "Jovana", "Milošević", null, true, null, "JOVANA@GMAIL.COM", "JOVANA@GMAIL.COM", null, "061100110", false, "a0b1c2d3-e4f5-4012-8002-000000000012", false, "jovana@gmail.com", "Patient" },
                    { "p9", 0, "a0b1c2d3-e4f5-4013-8001-000000000013", "stefan@gmail.com", false, "Stefan", "Đorđević", null, true, null, "STEFAN@GMAIL.COM", "STEFAN@GMAIL.COM", null, "061100111", false, "a0b1c2d3-e4f5-4013-8002-000000000013", false, "stefan@gmail.com", "Patient" },
                    { "ph1", 0, "a0b1c2d3-e4f5-4017-8001-000000000017", "pharmacist@pharmacy.com", false, "Milan", "Jovanović", "PH-L1", true, null, "PHARMACIST@PHARMACY.COM", "PHARMACIST@PHARMACY.COM", null, "061200200", false, "a0b1c2d3-e4f5-4017-8002-000000000017", false, "pharmacist@pharmacy.com", "Pharmacist" }
                });

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
                table: "Doctors",
                columns: new[] { "Id", "Address", "AppUserId", "BirthDate", "FirstName", "Gender", "LastName", "LicenseNumber", "PhoneNumber", "RegistrationStatus", "Specialization" },
                values: new object[,]
                {
                    { 1, "Address1", "d1", new DateTime(1995, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Marko", "Male", "Marković", "L1", "061100100", "Approved", "Cardiology" },
                    { 2, "Address2", "d2", new DateTime(1993, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Senad", "Male", "Husić", "L2", "061100101", "Approved", "Neurology" },
                    { 3, "Address3", "d3", new DateTime(1991, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Petar", "Male", "Petrović", "L3", "061100102", "Approved", "Psychiatry" }
                });

            migrationBuilder.InsertData(
                table: "Pharmacists",
                columns: new[] { "Id", "Address", "AppUserId", "CreatedAt", "FirstName", "HireDate", "LastName", "LicenseNumber", "PhoneNumber", "UpdatedAt" },
                values: new object[] { 1, "Apotekarska 15, Beograd", "ph1", new DateTime(2020, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Milan", new DateTime(2020, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Jovanović", "PH-L1", "061200200", null });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "Id", "Address", "AppUserId", "BloodType", "DateOfBirth", "DoctorId", "FirstName", "Gender", "IsAdmitted", "LastName", "PhoneNumber", "RegistrationStatus" },
                values: new object[,]
                {
                    { 1, "Address1", "p1", "A+", new DateTime(1990, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Ismet", "Male", null, "Horo", "061100103", "Pending" },
                    { 2, "Address2", "p2", "B+", new DateTime(1991, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Elon", "Female", null, "Musk", "061100104", "Pending" },
                    { 3, "Address3", "p3", "AB+", new DateTime(1992, 3, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Peter", "Other", null, "Griffin", "061100105", "Pending" },
                    { 4, "Bulevar Kralja Aleksandra 15", "p4", "O+", new DateTime(1988, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Ana", "Female", null, "Jovanović", "061100106", "Pending" },
                    { 5, "Knez Mihailova 25", "p5", "A-", new DateTime(1985, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Marko", "Male", null, "Nikolić", "061100107", "Pending" },
                    { 6, "Nemanjina 10", "p6", "B-", new DateTime(1993, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Sara", "Female", null, "Stojanović", "061100108", "Pending" },
                    { 7, "Terazije 5", "p7", "O-", new DateTime(1987, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Nikola", "Male", null, "Popović", "061100109", "Pending" },
                    { 8, "Vračar 20", "p8", "AB-", new DateTime(1994, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Jovana", "Female", null, "Milošević", "061100110", "Pending" },
                    { 9, "Svetog Save 45", "p9", "A+", new DateTime(1989, 2, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Stefan", "Male", null, "Đorđević", "061100111", "Pending" },
                    { 10, "Kralja Milana 30", "p10", "B+", new DateTime(1995, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Milica", "Female", null, "Radić", "061100112", "Pending" },
                    { 11, "Obilićev venac 12", "p11", "O+", new DateTime(1986, 8, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Luka", "Male", null, "Stefanović", "061100113", "Pending" },
                    { 12, "Dunavska 8", "p12", "AB+", new DateTime(1996, 4, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Teodora", "Female", null, "Lazić", "061100114", "Pending" }
                });

            migrationBuilder.InsertData(
                table: "MedicalRecords",
                columns: new[] { "Id", "PatientId", "RecordNumber" },
                values: new object[,]
                {
                    { 1, 1, "MRID1" },
                    { 2, 2, "MRID2" },
                    { 3, 3, "MRID3" },
                    { 4, 4, "MRID4" },
                    { 5, 5, "MRID5" },
                    { 6, 6, "MRID6" },
                    { 7, 7, "MRID7" },
                    { 8, 8, "MRID8" },
                    { 9, 9, "MRID9" },
                    { 10, 10, "MRID10" },
                    { 11, 11, "MRID11" },
                    { 12, 12, "MRID12" }
                });

            migrationBuilder.InsertData(
                table: "MedicalReports",
                columns: new[] { "Id", "CreatedAt", "Description", "Diagnosis", "DoctorId", "MedicalRecordId", "Symptoms", "Therapy" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Tension headache", 1, 1, "Headache, fatigue", "Rest, Paracetamol 500mg" },
                    { 2, new DateTime(2025, 11, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Hypertension", 1, 1, "High blood pressure", "Amlodipine 5mg" },
                    { 3, new DateTime(2025, 12, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Anxiety", 1, 1, "Chest pain", "Relaxation techniques" },
                    { 4, new DateTime(2026, 1, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Iron deficiency", 1, 1, "Fatigue, dizziness", "Iron supplements" },
                    { 5, new DateTime(2025, 10, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Pharyngitis", 1, 2, "Sore throat, fever", "Amoxicillin 500mg" },
                    { 6, new DateTime(2025, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Common cold", 1, 2, "Cough, runny nose", "Rest, fluids" },
                    { 7, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Lumbar strain", 1, 2, "Back pain", "Ibuprofen 400mg, physiotherapy" },
                    { 8, new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Gastroenteritis", 1, 2, "Nausea, vomiting", "Hydration, bland diet" },
                    { 9, new DateTime(2025, 9, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Arthritis", 1, 3, "Joint pain, swelling", "Diclofenac 50mg" },
                    { 10, new DateTime(2025, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Mild asthma", 1, 3, "Shortness of breath", "Inhaler prescribed" },
                    { 11, new DateTime(2026, 1, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Allergic reaction", 1, 3, "Skin rash, itching", "Cetirizine 10mg" },
                    { 12, new DateTime(2026, 2, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Stress-related insomnia", 1, 3, "Insomnia, anxiety", "Relaxation therapy" },
                    { 13, new DateTime(2025, 10, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Type 2 Diabetes", 1, 4, "Frequent urination, thirst", "Metformin 500mg" },
                    { 14, new DateTime(2025, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Hyperlipidemia", 1, 4, "High cholesterol", "Atorvastatin 20mg" },
                    { 15, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Diabetic check-up", 1, 4, "Blurred vision", "Continue Metformin" },
                    { 16, new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Thyroid check", 1, 4, "Weight gain, fatigue", "Blood tests ordered" },
                    { 17, new DateTime(2025, 10, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Migraine", 2, 5, "Migraine, light sensitivity", "Paracetamol, dark room rest" },
                    { 18, new DateTime(2025, 11, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Carpal tunnel syndrome", 2, 5, "Numbness in hands", "Wrist splint, rest" },
                    { 19, new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Stress-related cognitive issues", 2, 5, "Memory issues, confusion", "Stress management" },
                    { 20, new DateTime(2026, 2, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Vertigo", 2, 5, "Dizziness, nausea", "Epley maneuver" },
                    { 21, new DateTime(2025, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Otitis media", 2, 6, "Ear pain, hearing loss", "Amoxicillin 500mg" },
                    { 22, new DateTime(2025, 11, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Conjunctivitis", 2, 6, "Eye redness, discharge", "Antibiotic eye drops" },
                    { 23, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Grade 1 sprain", 2, 6, "Ankle sprain", "RICE method, Ibuprofen" },
                    { 24, new DateTime(2026, 2, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "IBS", 2, 6, "Stomach cramps", "Diet modification" },
                    { 25, new DateTime(2025, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Costochondritis", 2, 7, "Chest tightness", "Anti-inflammatory medication" },
                    { 26, new DateTime(2025, 12, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Osteoarthritis", 2, 7, "Knee pain, stiffness", "Physiotherapy, Diclofenac" },
                    { 27, new DateTime(2026, 1, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Vitamin D deficiency", 2, 7, "Hair loss, fatigue", "Vitamin D3 supplements" },
                    { 28, new DateTime(2026, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "GERD", 2, 7, "Acid reflux, heartburn", "Omeprazole 20mg" },
                    { 29, new DateTime(2025, 10, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Anxiety disorder", 3, 8, "Mood swings, irritability", "Therapy sessions" },
                    { 30, new DateTime(2025, 11, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Depression", 3, 8, "Sleep problems, fatigue", "CBT therapy" },
                    { 31, new DateTime(2026, 1, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Panic disorder", 3, 8, "Panic attacks", "Breathing exercises" },
                    { 32, new DateTime(2026, 2, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Social anxiety", 3, 8, "Social withdrawal", "Exposure therapy" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_AppUserId",
                table: "Doctors",
                column: "AppUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_PatientId",
                table: "Files",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PatientId",
                table: "MedicalRecords",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalReports_DoctorId",
                table: "MedicalReports",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalReports_MedicalRecordId",
                table: "MedicalReports",
                column: "MedicalRecordId");

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
                name: "IX_Medications_Name",
                table: "Medications",
                column: "Name");

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

            migrationBuilder.CreateIndex(
                name: "IX_Patients_AppUserId",
                table: "Patients",
                column: "AppUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_DoctorId",
                table: "Patients",
                column: "DoctorId");

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
                name: "IX_PrescriptionItems_PrescriptionId_MedicationId",
                table: "PrescriptionItems",
                columns: new[] { "PrescriptionId", "MedicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId",
                table: "Prescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_MedicalReportId",
                table: "Prescriptions",
                column: "MedicalReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId_Status",
                table: "Prescriptions",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PharmacistId",
                table: "Prescriptions",
                column: "PharmacistId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescribedDate",
                table: "Prescriptions",
                column: "PrescribedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PrescriptionNumber",
                table: "Prescriptions",
                column: "PrescriptionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_Status_CreatedAt",
                table: "Prescriptions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_Status_DispensedDate",
                table: "Prescriptions",
                columns: new[] { "Status", "DispensedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "MedicalReports");

            migrationBuilder.DropTable(
                name: "Pharmacists");

            migrationBuilder.DropTable(
                name: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
