using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;
using eBolnica.Domain.Entities.Pharmacy;
using System.Globalization;
using System.Text;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Csv;

internal static class MedicationCsvService
{
    public const int MaxExportRows = PharmacyExportLimits.MaxCsvExportRows;
    public const int MaxImportRows = 10_000;
    public const int MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly string[] ImportHeaders =
    [
        "Name", "Generic Name", "Category", "Manufacturer", "Description",
        "Price", "Stock Quantity", "Minimum Stock Level", "Expiry Date",
        "Batch Number", "Dosage Form", "Strength", "Requires Prescription", "Active"
    ];

    public static string BuildExportCsv(IEnumerable<MedicationEntity> medications)
    {
        var headers = ImportHeaders.Concat(["Status"]).ToArray();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers));

        foreach (var m in medications)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Escape(m.Name), Escape(m.GenericName), Escape(m.Category), Escape(m.Manufacturer),
                Escape(m.Description), m.Price.ToString(CultureInfo.InvariantCulture),
                m.StockQuantity.ToString(CultureInfo.InvariantCulture),
                m.MinimumStockLevel.ToString(CultureInfo.InvariantCulture),
                FormatDate(m.ExpiryDate), Escape(m.BatchNumber), Escape(m.DosageForm),
                Escape(m.Strength), m.RequiresPrescription ? "Yes" : "No",
                m.IsActive ? "Yes" : "No", Escape(GetStatusLabel(m))
            }));
        }

        return sb.ToString();
    }

    public static string BuildImportTemplateCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", ImportHeaders));
        sb.AppendLine(string.Join(",", new[]
        {
            Escape("Paracetamol"), Escape("Acetaminophen"), Escape("Analgesics"),
            Escape("PharmaCorp"), Escape("Pain reliever"), "9.99", "100", "20",
            "2026-12-31", Escape("BATCH-001"), Escape("Tablet"), Escape("500mg"),
            "No", "Yes"
        }));
        return sb.ToString();
    }

    public static string GetExportFileName() =>
        $"pharmacy-medications-{DateTime.UtcNow:yyyy-MM-dd}.csv";

    public static string GetImportTemplateFileName() => "medication-import-template.csv";

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

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
