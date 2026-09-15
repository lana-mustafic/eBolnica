using CsvHelper;
using CsvHelper.Configuration;
using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Domain.Entities.Pharmacy;
using System.Globalization;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Csv;

internal static class MedicationCsvService
{
    public const int MaxExportRows = PharmacyExportLimits.MaxCsvExportRows;
    public const int MaxImportRows = 10_000;
    public const int MaxFileSizeBytes = 5 * 1024 * 1024;

    internal static readonly string[] ImportHeaders =
    [
        "Name", "Generic Name", "Category", "Manufacturer", "Description",
        "Price", "Stock Quantity", "Minimum Stock Level", "Expiry Date",
        "Batch Number", "Dosage Form", "Strength", "Requires Prescription", "Active"
    ];

    public static string BuildExportCsv(IEnumerable<MedicationEntity> medications)
    {
        var rows = new List<string[]> { ImportHeaders.Concat(["Status"]).ToArray() };
        rows.AddRange(medications.Select(ToExportRow));
        return WriteRows(rows);
    }

    public static string BuildImportTemplateCsv()
    {
        var expiry = DateTime.UtcNow.Date.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return WriteRows(
        [
            ImportHeaders,
            [
                "Paracetamol", "Acetaminophen", "Analgetici", "PharmaCorp",
                "Lijek protiv bolova", "9.99", "100", "20", expiry,
                "BATCH-001", "Tableta", "500mg", "No", "Yes"
            ]
        ]);
    }

    public static string GetExportFileName() =>
        $"pharmacy-medications-{DateTime.UtcNow:yyyy-MM-dd}.csv";

    public static string GetImportTemplateFileName() => "medication-import-template.csv";

    public static List<string[]> ParseRows(string content)
    {
        var rows = new List<string[]>();
        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, CreateCsvConfiguration());
        while (csv.Read())
        {
            var count = csv.Parser.Count;
            var cells = new string[count];
            for (var i = 0; i < count; i++)
                cells[i] = csv.GetField(i) ?? string.Empty;

            if (cells.All(string.IsNullOrWhiteSpace))
                continue;

            rows.Add(cells);
        }

        return rows;
    }

    public static string WriteRows(IEnumerable<IReadOnlyList<string>> rows)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CreateCsvConfiguration(), leaveOpen: true);
        foreach (var row in rows)
        {
            foreach (var cell in row)
                csv.WriteField(cell);
            csv.NextRecord();
        }

        csv.Flush();
        return writer.ToString();
    }

    private static CsvConfiguration CreateCsvConfiguration() => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        IgnoreBlankLines = true,
        MissingFieldFound = null,
        BadDataFound = null,
        Mode = CsvMode.RFC4180,
        TrimOptions = TrimOptions.None
    };

    private static string[] ToExportRow(MedicationEntity m) =>
    [
        m.Name,
        m.GenericName ?? string.Empty,
        MedicationCategoryAliases.ToBosnian(m.Category) ?? string.Empty,
        m.Manufacturer ?? string.Empty,
        m.Description ?? string.Empty,
        m.Price.ToString(CultureInfo.InvariantCulture),
        m.StockQuantity.ToString(CultureInfo.InvariantCulture),
        m.MinimumStockLevel.ToString(CultureInfo.InvariantCulture),
        FormatDate(m.ExpiryDate),
        m.BatchNumber ?? string.Empty,
        MedicationDosageFormAliases.ToBosnian(m.DosageForm) ?? string.Empty,
        m.Strength ?? string.Empty,
        m.RequiresPrescription ? "Yes" : "No",
        m.IsActive ? "Yes" : "No",
        GetStatusLabel(m)
    ];

    private static string GetStatusLabel(MedicationEntity m)
    {
        if (!m.IsActive) return "Inactive";
        if (m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date < DateTime.UtcNow.Date) return "Expired";
        if (m.StockQuantity == 0) return "Out of Stock";
        if (m.StockQuantity < m.MinimumStockLevel) return "Low Stock";
        return "Active";
    }

    private static string FormatDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
}
