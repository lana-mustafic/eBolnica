using System.ComponentModel.DataAnnotations;
using eBolnicaAPI.Models.DTOs;

namespace eBolnicaAPI.Services
{
    public enum PharmacyListEndpoint
    {
        Medications,
        Prescriptions,
        Inventory
    }

    /// <summary>
    /// Validates sortBy values for pharmacy list endpoints.
    /// </summary>
    public static class PharmacySortValidator
    {
        private static readonly HashSet<string> MedicationSortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "name",
            "price",
            "datecreated",
            "createdat",
            "stockquantity",
            "stock",
            "category",
            "expirydate",
            "expiry"
        };

        private static readonly HashSet<string> PrescriptionSortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "datecreated",
            "createdat",
            "totalamount",
            "amount",
            "prescriptionnumber",
            "number",
            "status",
            "prescribeddate"
        };

        public static IReadOnlyList<ValidationResult> Validate(
            PharmacyQueryParameters parameters,
            PharmacyListEndpoint endpoint)
        {
            var results = new List<ValidationResult>();

            if (string.IsNullOrWhiteSpace(parameters.SortBy))
            {
                return results;
            }

            var allowedColumns = endpoint == PharmacyListEndpoint.Prescriptions
                ? PrescriptionSortColumns
                : MedicationSortColumns;

            var invalidColumns = ParseSortColumns(parameters.SortBy)
                .Where(column => !allowedColumns.Contains(column))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (invalidColumns.Count == 0)
            {
                return results;
            }

            results.Add(new ValidationResult(
                $"Unknown sort column(s): {string.Join(", ", invalidColumns)}. Supported values: {FormatSupportedColumns(allowedColumns)}",
                new[] { nameof(PharmacyQueryParameters.SortBy) }));

            return results;
        }

        internal static IEnumerable<string> ParseSortColumns(string sortBy)
        {
            foreach (var part in sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var column = part.Trim();

                if (column.Contains(':'))
                {
                    column = column.Split(':', 2, StringSplitOptions.TrimEntries)[0];
                }

                if (!string.IsNullOrWhiteSpace(column))
                {
                    yield return column;
                }
            }
        }

        private static string FormatSupportedColumns(IEnumerable<string> columns)
        {
            return string.Join(", ", columns.OrderBy(column => column, StringComparer.OrdinalIgnoreCase));
        }
    }
}
