using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMorePatients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p4')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p4', 0, N'fixed-guid-1', N'ana@gmail.com', CAST(0 AS bit), N'Ana', N'Jovanović', NULL, CAST(1 AS bit), NULL, N'ANA@GMAIL.COM', N'ANA@GMAIL.COM', NULL, N'061100106', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'ana@gmail.com', N'Patient');
                
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p5')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p5', 0, N'fixed-guid-1', N'marko.patient@gmail.com', CAST(0 AS bit), N'Marko', N'Nikolić', NULL, CAST(1 AS bit), NULL, N'MARKO.PATIENT@GMAIL.COM', N'MARKO.PATIENT@GMAIL.COM', NULL, N'061100107', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'marko.patient@gmail.com', N'Patient');
                
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p6')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p6', 0, N'fixed-guid-1', N'sara@gmail.com', CAST(0 AS bit), N'Sara', N'Stojanović', NULL, CAST(1 AS bit), NULL, N'SARA@GMAIL.COM', N'SARA@GMAIL.COM', NULL, N'061100108', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'sara@gmail.com', N'Patient');
                
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p7')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p7', 0, N'fixed-guid-1', N'nikola@gmail.com', CAST(0 AS bit), N'Nikola', N'Popović', NULL, CAST(1 AS bit), NULL, N'NIKOLA@GMAIL.COM', N'NIKOLA@GMAIL.COM', NULL, N'061100109', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'nikola@gmail.com', N'Patient');
                
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p8')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p8', 0, N'fixed-guid-1', N'jovana@gmail.com', CAST(0 AS bit), N'Jovana', N'Milošević', NULL, CAST(1 AS bit), NULL, N'JOVANA@GMAIL.COM', N'JOVANA@GMAIL.COM', NULL, N'061100110', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'jovana@gmail.com', N'Patient');
                
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p9')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p9', 0, N'fixed-guid-1', N'stefan@gmail.com', CAST(0 AS bit), N'Stefan', N'Đorđević', NULL, CAST(1 AS bit), NULL, N'STEFAN@GMAIL.COM', N'STEFAN@GMAIL.COM', NULL, N'061100111', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'stefan@gmail.com', N'Patient');
                
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p10')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p10', 0, N'fixed-guid-1', N'milica@gmail.com', CAST(0 AS bit), N'Milica', N'Radić', NULL, CAST(1 AS bit), NULL, N'MILICA@GMAIL.COM', N'MILICA@GMAIL.COM', NULL, N'061100112', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'milica@gmail.com', N'Patient');
                
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p11')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p11', 0, N'fixed-guid-1', N'luka@gmail.com', CAST(0 AS bit), N'Luka', N'Stefanović', NULL, CAST(1 AS bit), NULL, N'LUKA@GMAIL.COM', N'LUKA@GMAIL.COM', NULL, N'061100113', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'luka@gmail.com', N'Patient');
                
                IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = 'p12')
                INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [FirstName], [LastName], [LicenseNumber], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp], [TwoFactorEnabled], [UserName], [UserType])
                VALUES (N'p12', 0, N'fixed-guid-1', N'teodora@gmail.com', CAST(0 AS bit), N'Teodora', N'Lazić', NULL, CAST(1 AS bit), NULL, N'TEODORA@GMAIL.COM', N'TEODORA@GMAIL.COM', NULL, N'061100114', CAST(0 AS bit), N'fixed-guid-2', CAST(0 AS bit), N'teodora@gmail.com', N'Patient');
            ");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 1,
                column: "BloodType",
                value: "A+");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 2,
                column: "BloodType",
                value: "B+");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BloodType", "DateOfBirth", "Gender" },
                values: new object[] { "AB+", new DateTime(1992, 3, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "Other" });

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [Patients] ON;
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 4)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (4, N'Bulevar Kralja Aleksandra 15', N'p4', N'O+', '1988-05-20T00:00:00.0000000', 1, N'Ana', N'Female', NULL, N'Jovanović', N'MRID4', N'061100106');
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 5)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (5, N'Knez Mihailova 25', N'p5', N'A-', '1985-07-10T00:00:00.0000000', 2, N'Marko', N'Male', NULL, N'Nikolić', N'MRID5', N'061100107');
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 6)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (6, N'Nemanjina 10', N'p6', N'B-', '1993-09-05T00:00:00.0000000', 2, N'Sara', N'Female', NULL, N'Stojanović', N'MRID6', N'061100108');
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 7)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (7, N'Terazije 5', N'p7', N'O-', '1987-11-18T00:00:00.0000000', 2, N'Nikola', N'Male', NULL, N'Popović', N'MRID7', N'061100109');
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 8)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (8, N'Vračar 20', N'p8', N'AB-', '1994-12-25T00:00:00.0000000', 3, N'Jovana', N'Female', NULL, N'Milošević', N'MRID8', N'061100110');
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 9)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (9, N'Svetog Save 45', N'p9', N'A+', '1989-02-12T00:00:00.0000000', 1, N'Stefan', N'Male', NULL, N'Đorđević', N'MRID9', N'061100111');
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 10)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (10, N'Kralja Milana 30', N'p10', N'B+', '1995-06-08T00:00:00.0000000', 1, N'Milica', N'Female', NULL, N'Radić', N'MRID10', N'061100112');
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 11)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (11, N'Obilićev venac 12', N'p11', N'O+', '1986-08-22T00:00:00.0000000', 1, N'Luka', N'Male', NULL, N'Stefanović', N'MRID11', N'061100113');
                
                IF NOT EXISTS (SELECT 1 FROM [Patients] WHERE [Id] = 12)
                INSERT INTO [Patients] ([Id], [Address], [AppUserId], [BloodType], [DateOfBirth], [DoctorId], [FirstName], [Gender], [IsAdmitted], [LastName], [MedicalRecordId], [PhoneNumber])
                VALUES (12, N'Dunavska 8', N'p12', N'AB+', '1996-04-30T00:00:00.0000000', 1, N'Teodora', N'Female', NULL, N'Lazić', N'MRID12', N'061100114');
                
                SET IDENTITY_INSERT [Patients] OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p10");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p11");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p12");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p4");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p5");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p6");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p7");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p8");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "p9");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 1,
                column: "BloodType",
                value: "A");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 2,
                column: "BloodType",
                value: "B");

            migrationBuilder.UpdateData(
                table: "Patients",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BloodType", "DateOfBirth", "Gender" },
                values: new object[] { "0", new DateTime(1992, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Male" });
        }
    }
}
