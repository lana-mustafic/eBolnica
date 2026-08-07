using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMedicationStockHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO MedicationStockHistory (MedicationId, ChangeQuantity, StockAfter, Reason, IsDeleted, CreatedAtUtc)
                SELECT m.Id, m.StockQuantity, m.StockQuantity, 'InitialStock', 0, m.CreatedAtUtc
                FROM Medications m
                WHERE m.IsDeleted = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM MedicationStockHistory h
                      WHERE h.MedicationId = m.Id AND h.IsDeleted = 0)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM MedicationStockHistory
                WHERE Reason = 'InitialStock'
                  AND PrescriptionId IS NULL
                  AND ChangeQuantity = StockAfter
                """);
        }
    }
}
