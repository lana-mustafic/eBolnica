using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationImageThumbnailUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailRelativeUrl",
                table: "MedicationImages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailRelativeUrl",
                table: "MedicationImages");
        }
    }
}
