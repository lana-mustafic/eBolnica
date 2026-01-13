using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class Update1 : Migration
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
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    MedicalRecordId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAdmitted = table.Column<bool>(type: "bit", nullable: true)
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
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symptoms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Therapy = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "FirstName", "LastName", "LicenseNumber", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName", "UserType" },
                values: new object[,]
                {
                    { "a1", 0, "fixed-guid-3", "admin@gmail.com", true, "Admin", "User", null, true, null, "ADMIN@GMAIL.COM", "ADMIN@GMAIL.COM", null, "061000000", true, "fixed-guid-3", false, "admin@gmail.com", "Admin" },
                    { "d1", 0, "fixed-guid-1", "marko@gmail.com", false, "Marko", "Marković", "L1", true, null, "MARKO@GMAIL.COM", "MARKO@GMAIL.COM", null, "061100100", false, "fixed-guid-2", false, "marko@gmail.com", "Doctor" },
                    { "d2", 0, "fixed-guid-1", "senad@gmail.com", false, "Senad", "Husić", "L2", true, null, "SENAD@GMAIL.COM", "SENAD@GMAIL.COM", null, "061100101", false, "fixed-guid-2", false, "senad@gmail.com", "Doctor" },
                    { "d3", 0, "fixed-guid-1", "petar@gmail.com", false, "Petar", "Petrović", "L3", true, null, "PETAR@GMAIL.COM", "PETAR@GMAIL.COM", null, "061100102", false, "fixed-guid-2", false, "petar@gmail.com", "Doctor" },
                    { "p1", 0, "fixed-guid-1", "ismet@gmail.com", false, "Ismet", "Horo", null, true, null, "ISMET@GMAIL.COM", "ISMET@GMAIL.COM", null, "061100103", false, "fixed-guid-2", false, "ismet@gmail.com", "Patient" },
                    { "p10", 0, "fixed-guid-1", "milica@gmail.com", false, "Milica", "Radić", null, true, null, "MILICA@GMAIL.COM", "MILICA@GMAIL.COM", null, "061100112", false, "fixed-guid-2", false, "milica@gmail.com", "Patient" },
                    { "p11", 0, "fixed-guid-1", "luka@gmail.com", false, "Luka", "Stefanović", null, true, null, "LUKA@GMAIL.COM", "LUKA@GMAIL.COM", null, "061100113", false, "fixed-guid-2", false, "luka@gmail.com", "Patient" },
                    { "p12", 0, "fixed-guid-1", "teodora@gmail.com", false, "Teodora", "Lazić", null, true, null, "TEODORA@GMAIL.COM", "TEODORA@GMAIL.COM", null, "061100114", false, "fixed-guid-2", false, "teodora@gmail.com", "Patient" },
                    { "p2", 0, "fixed-guid-1", "elon@gmail.com", false, "Elon", "Musk", null, true, null, "ELON@GMAIL.COM", "ELON@GMAIL.COM", null, "061100104", false, "fixed-guid-2", false, "elon@gmail.com", "Patient" },
                    { "p3", 0, "fixed-guid-1", "peter@gmail.com", false, "Peter", "Griffin", null, true, null, "PETER@GMAIL.COM", "PETER@GMAIL.COM", null, "061100105", false, "fixed-guid-2", false, "peter@gmail.com", "Patient" },
                    { "p4", 0, "fixed-guid-1", "ana@gmail.com", false, "Ana", "Jovanović", null, true, null, "ANA@GMAIL.COM", "ANA@GMAIL.COM", null, "061100106", false, "fixed-guid-2", false, "ana@gmail.com", "Patient" },
                    { "p5", 0, "fixed-guid-1", "marko.patient@gmail.com", false, "Marko", "Nikolić", null, true, null, "MARKO.PATIENT@GMAIL.COM", "MARKO.PATIENT@GMAIL.COM", null, "061100107", false, "fixed-guid-2", false, "marko.patient@gmail.com", "Patient" },
                    { "p6", 0, "fixed-guid-1", "sara@gmail.com", false, "Sara", "Stojanović", null, true, null, "SARA@GMAIL.COM", "SARA@GMAIL.COM", null, "061100108", false, "fixed-guid-2", false, "sara@gmail.com", "Patient" },
                    { "p7", 0, "fixed-guid-1", "nikola@gmail.com", false, "Nikola", "Popović", null, true, null, "NIKOLA@GMAIL.COM", "NIKOLA@GMAIL.COM", null, "061100109", false, "fixed-guid-2", false, "nikola@gmail.com", "Patient" },
                    { "p8", 0, "fixed-guid-1", "jovana@gmail.com", false, "Jovana", "Milošević", null, true, null, "JOVANA@GMAIL.COM", "JOVANA@GMAIL.COM", null, "061100110", false, "fixed-guid-2", false, "jovana@gmail.com", "Patient" },
                    { "p9", 0, "fixed-guid-1", "stefan@gmail.com", false, "Stefan", "Đorđević", null, true, null, "STEFAN@GMAIL.COM", "STEFAN@GMAIL.COM", null, "061100111", false, "fixed-guid-2", false, "stefan@gmail.com", "Patient" }
                });

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "Address", "AppUserId", "BirthDate", "FirstName", "LastName", "LicenseNumber", "PhoneNumber", "RegistrationStatus", "Specialization" },
                values: new object[,]
                {
                    { 1, "Address1", "d1", new DateTime(1995, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Marko", "Marković", "L1", "061100100", "Approved", "Cardiology" },
                    { 2, "Address2", "d2", new DateTime(1993, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Senad", "Husić", "L2", "061100101", "Approved", "Neurology" },
                    { 3, "Address3", "d3", new DateTime(1991, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Petar", "Petrović", "L3", "061100102", "Approved", "Psychiatry" }
                });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "Id", "Address", "AppUserId", "BloodType", "DateOfBirth", "DoctorId", "FirstName", "Gender", "IsAdmitted", "LastName", "MedicalRecordId", "PhoneNumber" },
                values: new object[,]
                {
                    { 1, "Address1", "p1", "A+", new DateTime(1990, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Ismet", "Male", null, "Horo", "MRID1", "061100103" },
                    { 2, "Address2", "p2", "B+", new DateTime(1991, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Elon", "Female", null, "Musk", "MRID2", "061100104" },
                    { 3, "Address3", "p3", "AB+", new DateTime(1992, 3, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Peter", "Other", null, "Griffin", "MRID3", "061100105" },
                    { 4, "Bulevar Kralja Aleksandra 15", "p4", "O+", new DateTime(1988, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Ana", "Female", null, "Jovanović", "MRID4", "061100106" },
                    { 5, "Knez Mihailova 25", "p5", "A-", new DateTime(1985, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Marko", "Male", null, "Nikolić", "MRID5", "061100107" },
                    { 6, "Nemanjina 10", "p6", "B-", new DateTime(1993, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Sara", "Female", null, "Stojanović", "MRID6", "061100108" },
                    { 7, "Terazije 5", "p7", "O-", new DateTime(1987, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Nikola", "Male", null, "Popović", "MRID7", "061100109" },
                    { 8, "Vračar 20", "p8", "AB-", new DateTime(1994, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Jovana", "Female", null, "Milošević", "MRID8", "061100110" },
                    { 9, "Svetog Save 45", "p9", "A+", new DateTime(1989, 2, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Stefan", "Male", null, "Đorđević", "MRID9", "061100111" },
                    { 10, "Kralja Milana 30", "p10", "B+", new DateTime(1995, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Milica", "Female", null, "Radić", "MRID10", "061100112" },
                    { 11, "Obilićev venac 12", "p11", "O+", new DateTime(1986, 8, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Luka", "Male", null, "Stefanović", "MRID11", "061100113" },
                    { 12, "Dunavska 8", "p12", "AB+", new DateTime(1996, 4, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Teodora", "Female", null, "Lazić", "MRID12", "061100114" }
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
                name: "IX_Patients_AppUserId",
                table: "Patients",
                column: "AppUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_DoctorId",
                table: "Patients",
                column: "DoctorId");
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
                name: "MedicalReports");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

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
