using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eBolnica.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrescriptionNumberSequences",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionNumberSequences", x => x.Year);
                });

            migrationBuilder.Sql("""
                INSERT INTO PrescriptionNumberSequences ([Year], LastNumber)
                SELECT
                    CAST(PARSENAME(REPLACE(PrescriptionNumber, '-', '.'), 2) AS INT) AS [Year],
                    MAX(CAST(PARSENAME(REPLACE(PrescriptionNumber, '-', '.'), 1) AS INT)) AS LastNumber
                FROM Prescriptions
                WHERE PrescriptionNumber LIKE 'RX-[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]'
                GROUP BY CAST(PARSENAME(REPLACE(PrescriptionNumber, '-', '.'), 2) AS INT)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrescriptionNumberSequences");
        }
    }
}
