using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MedicationPrimaryImageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimaryImageId",
                table: "Medications",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE m
                SET
                    PrimaryImageId = pi.Id,
                    ImageUrl = COALESCE(m.ImageUrl, pi.RelativeUrl)
                FROM Medications m
                CROSS APPLY (
                    SELECT TOP 1 i.Id, i.RelativeUrl
                    FROM MedicationImages i
                    WHERE i.MedicationId = m.Id AND i.IsDeleted = 0
                    ORDER BY i.IsPrimary DESC, i.SortOrder ASC
                ) pi
                WHERE m.IsDeleted = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Medications_PrimaryImageId",
                table: "Medications",
                column: "PrimaryImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_MedicationImages_PrimaryImageId",
                table: "Medications",
                column: "PrimaryImageId",
                principalTable: "MedicationImages",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medications_MedicationImages_PrimaryImageId",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Medications_PrimaryImageId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "PrimaryImageId",
                table: "Medications");
        }
    }
}
