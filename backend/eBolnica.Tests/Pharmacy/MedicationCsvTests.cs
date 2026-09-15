using System.Globalization;
using eBolnica.Application.Modules.Pharmacy.Medications.Csv;
using eBolnica.Domain.Entities.Pharmacy;
using Xunit;

namespace eBolnica.Tests.Pharmacy;

public class MedicationCsvTests
{
    private static readonly string[] Headers =
    [
        "Name", "Generic Name", "Category", "Manufacturer", "Description",
        "Price", "Stock Quantity", "Minimum Stock Level", "Expiry Date",
        "Batch Number", "Dosage Form", "Strength", "Requires Prescription", "Active"
    ];

    [Fact]
    public void ParseRows_QuotedMultilineField_KeepsASingleDataRow()
    {
        var csv = """
            Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
            "Test Med","Generic","Analgetici","Maker","Line1
            Line2",9.99,10,2,2099-01-01,B1,Tableta,10mg,Yes,Yes
            """;

        var rows = MedicationCsvImporter.ParseRows(csv);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Line1\nLine2", rows[1][4].Replace("\r\n", "\n"));
    }

    [Fact]
    public void ExportThenImport_DescriptionWithNewline_RoundTrips()
    {
        var medication = new MedicationEntity
        {
            Name = "Round Trip Med",
            GenericName = "Generic",
            Category = "Analgetici",
            Manufacturer = "Maker",
            Description = "First line\nSecond line",
            Price = 9.99m,
            StockQuantity = 10,
            MinimumStockLevel = 2,
            ExpiryDate = DateTime.UtcNow.Date.AddYears(1),
            BatchNumber = "B1",
            DosageForm = "Tableta",
            Strength = "10mg",
            RequiresPrescription = true,
            IsActive = true
        };

        var csv = MedicationCsvService.BuildExportCsv([medication]);
        var rows = MedicationCsvImporter.ParseRows(csv);
        var columns = MedicationCsvImporter.MapHeaders(rows[0]);

        Assert.Equal(2, rows.Count);
        Assert.True(MedicationCsvImporter.TryMapRow(2, rows[1], columns, out var command, out var error));
        Assert.Null(error);
        Assert.Equal("First line\nSecond line", command!.Description?.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TryMapRow_PriceAboveCreateLimit_FailsWithCreateValidator()
    {
        var columns = MedicationCsvImporter.MapHeaders(Headers);

        var mapped = MedicationCsvImporter.TryMapRow(2, ValidRow(price: "20000"), columns, out var command, out var error);

        Assert.False(mapped);
        Assert.Null(command);
        Assert.Equal("Price", error!.Field);
        Assert.Contains("10000", error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void TryMapRow_NameLongerThanCreateLimit_Fails()
    {
        var columns = MedicationCsvImporter.MapHeaders(Headers);

        var mapped = MedicationCsvImporter.TryMapRow(
            2,
            ValidRow(name: new string('A', 101)),
            columns,
            out var command,
            out var error);

        Assert.False(mapped);
        Assert.Null(command);
        Assert.Equal("Name", error!.Field);
    }

    [Fact]
    public void BuildImportTemplateCsv_UsesAFutureExpiryDate()
    {
        var csv = MedicationCsvService.BuildImportTemplateCsv();
        var rows = MedicationCsvImporter.ParseRows(csv);
        var expiryIndex = Array.FindIndex(rows[0], h => h.Equals("Expiry Date", StringComparison.OrdinalIgnoreCase));
        var expiry = DateTime.ParseExact(rows[1][expiryIndex], "yyyy-MM-dd", CultureInfo.InvariantCulture);

        Assert.True(expiry.Date > DateTime.UtcNow.Date);
    }

    private static string[] ValidRow(string? price = null, string? name = null) =>
    [
        name ?? "Valid Medication",
        "Generic",
        "Analgetici",
        "Maker",
        "Description",
        price ?? "9.99",
        "10",
        "2",
        DateTime.UtcNow.Date.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        "B1",
        "Tableta",
        "10mg",
        "Yes",
        "Yes"
    ];
}
