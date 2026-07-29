using eBolnicaAPI.Models.Entities;
using System.Globalization;
using System.Text;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// CSV export builder for medications (import-compatible columns + derived Status).
    /// </summary>
    public class MedicationCsvExportService : IMedicationCsvExportService
    {
        public int MaxExportRows { get; } = 10_000;

        private static readonly string[] ImportHeaders =
        {
            "Name",
            "Generic Name",
            "Category",
            "Manufacturer",
            "Description",
            "Price",
            "Stock Quantity",
            "Minimum Stock Level",
            "Expiry Date",
            "Batch Number",
            "Dosage Form",
            "Strength",
            "Requires Prescription",
            "Active"
        };

        private static readonly string[] ExportHeaders =
        [
            .. ImportHeaders,
            "Status"
        ];

        private static readonly string[] ImportTemplateExampleRow =
        {
            "Paracetamol (required, 3-100 characters)",
            "Acetaminophen (optional)",
            "Painkillers (required)",
            "PharmaCorp (optional)",
            "Pain reliever (optional, max 500 characters)",
            "9.99 (required, > 0)",
            "100 (required, integer >= 0)",
            "20 (required, integer >= 0)",
            "2026-12-31 (required, YYYY-MM-DD, must be future date)",
            "BATCH-001 (optional)",
            "Tablet (optional)",
            "500mg (optional)",
            "No (required: Yes or No)",
            "Yes (required: Yes or No)"
        };

        public string BuildCsv(IEnumerable<Medication> medications)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", ExportHeaders));

            foreach (var medication in medications)
            {
                builder.AppendLine(string.Join(",", new[]
                {
                    Escape(medication.Name),
                    Escape(medication.GenericName),
                    Escape(medication.Category),
                    Escape(medication.Manufacturer),
                    Escape(medication.Description),
                    medication.Price.ToString(CultureInfo.InvariantCulture),
                    medication.StockQuantity.ToString(CultureInfo.InvariantCulture),
                    medication.MinimumStockLevel.ToString(CultureInfo.InvariantCulture),
                    FormatDate(medication.ExpiryDate),
                    Escape(medication.BatchNumber),
                    Escape(medication.DosageForm),
                    Escape(medication.Strength),
                    medication.RequiresPrescription ? "Yes" : "No",
                    medication.IsActive ? "Yes" : "No",
                    Escape(GetStockStatusLabel(medication))
                }));
            }

            return builder.ToString();
        }

        public string GetExportFileName(DateTime? timestamp = null)
        {
            var day = (timestamp ?? DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return $"pharmacy-medications-{day}.csv";
        }

        public string BuildImportTemplateCsv()
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", ImportHeaders));
            builder.AppendLine(string.Join(",", ImportTemplateExampleRow.Select(Escape)));
            return builder.ToString();
        }

        public string GetImportTemplateFileName() => "medication-import-template.csv";

        internal static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        internal static string FormatDate(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        internal static string GetStockStatusLabel(Medication medication)
        {
            if (!medication.IsActive)
            {
                return "Inactive";
            }

            if (medication.ExpiryDate.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                if (medication.ExpiryDate.Value.Date < today)
                {
                    return "Expired";
                }
            }

            if (medication.StockQuantity == 0)
            {
                return "Out of Stock";
            }

            if (medication.StockQuantity < medication.MinimumStockLevel)
            {
                return "Low Stock";
            }

            return "Active";
        }
    }
}
