using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_AspNetUsers_AppUserId",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_AspNetUsers_AppUserId",
                table: "Patients");

            migrationBuilder.AddColumn<int>(
                name: "DoctorId",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppUserId1",
                table: "Doctors",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_DoctorId",
                table: "Patients",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_AppUserId1",
                table: "Doctors",
                column: "AppUserId1",
                unique: true,
                filter: "[AppUserId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_AspNetUsers_AppUserId",
                table: "Doctors",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_AspNetUsers_AppUserId1",
                table: "Doctors",
                column: "AppUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_AspNetUsers_AppUserId",
                table: "Patients",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Doctors_DoctorId",
                table: "Patients",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_AspNetUsers_AppUserId",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_AspNetUsers_AppUserId1",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_AspNetUsers_AppUserId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Doctors_DoctorId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_DoctorId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_AppUserId1",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "AppUserId1",
                table: "Doctors");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_AspNetUsers_AppUserId",
                table: "Doctors",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_AspNetUsers_AppUserId",
                table: "Patients",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
