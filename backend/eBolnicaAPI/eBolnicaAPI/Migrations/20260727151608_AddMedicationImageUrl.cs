using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnicaAPI.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// PHA-5-1-T8: Adds nullable ImageUrl column to Medications for primary image URL storage.
    /// Existing rows default to NULL; backfills from MedicationImages where available.
    /// </summary>
    public partial class AddMedicationImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Medications",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                defaultValue: null);

            migrationBuilder.Sql("""
                UPDATE m
                SET m.ImageUrl = src.RelativeUrl
                FROM Medications m
                INNER JOIN (
                    SELECT
                        mi.MedicationId,
                        mi.RelativeUrl,
                        ROW_NUMBER() OVER (
                            PARTITION BY mi.MedicationId
                            ORDER BY mi.IsPrimary DESC, mi.SortOrder
                        ) AS RowNum
                    FROM MedicationImages mi
                ) AS src ON src.MedicationId = m.Id AND src.RowNum = 1
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Medications");
        }
    }
}
