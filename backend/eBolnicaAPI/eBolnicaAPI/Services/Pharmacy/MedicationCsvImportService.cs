using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Parses and imports medication CSV files (multipart upload).
    /// </summary>
    public class MedicationCsvImportService : IMedicationCsvImportService
    {
        private static readonly string[] RequiredHeaders =
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

        private static readonly HashSet<string> IgnoredHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Status"
        };

        private readonly AppDbContext _context;
        private readonly IPharmacyAnalyticsService _analyticsService;

        public MedicationCsvImportService(AppDbContext context, IPharmacyAnalyticsService analyticsService)
        {
            _context = context;
            _analyticsService = analyticsService;
        }

        public int MaxFileSizeBytes { get; } = 5 * 1024 * 1024;

        public int MaxImportRows { get; } = 10_000;

        public async Task<(string? FileError, MedicationImportSummaryDto? Summary)> ImportAsync(
            IFormFile? file,
            CancellationToken cancellationToken = default)
        {
            var fileError = ValidateFile(file);
            if (fileError != null)
            {
                return (fileError, null);
            }

            string content;
            await using (var stream = file!.OpenReadStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                content = await reader.ReadToEndAsync(cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return ("The uploaded file is empty.", null);
            }

            IReadOnlyList<string[]> rows;
            try
            {
                rows = CsvParser.Parse(content);
            }
            catch (FormatException ex)
            {
                return ($"Malformed CSV file: {ex.Message}", null);
            }

            if (rows.Count == 0)
            {
                return ("The uploaded file is empty.", null);
            }

            var headerRow = rows[0];
            var headerMapResult = MapHeaders(headerRow);
            if (headerMapResult.Error != null)
            {
                return (headerMapResult.Error, null);
            }

            var dataRows = rows.Skip(1)
                .Select((cells, index) => new CsvDataRow(index + 2, cells))
                .Where(row => !IsBlankRow(row.Cells))
                .ToList();

            if (dataRows.Count > MaxImportRows)
            {
                return ($"Import is limited to {MaxImportRows} data rows. Split the file and try again.", null);
            }

            var summary = new MedicationImportSummaryDto
            {
                TotalRows = dataRows.Count
            };

            if (dataRows.Count == 0)
            {
                return (null, summary);
            }

            var existingNames = await _context.Medications
                .AsNoTracking()
                .Select(m => m.Name.ToLower())
                .ToListAsync(cancellationToken);
            var reservedNames = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
            var medicationsToInsert = new List<Medication>();

            foreach (var row in dataRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!TryParseRow(row, headerMapResult.ColumnIndexes!, out var parsed, out var rowErrors))
                {
                    summary.Errors.AddRange(rowErrors);
                    summary.FailureCount++;
                    continue;
                }

                if (reservedNames.Contains(parsed!.Name))
                {
                    summary.Errors.Add(new MedicationImportRowErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Field = "Name",
                        Value = parsed.Name,
                        Reason = "A medication with this name already exists."
                    });
                    summary.FailureCount++;
                    continue;
                }

                reservedNames.Add(parsed.Name);
                medicationsToInsert.Add(parsed.ToEntity());
                summary.SuccessCount++;
            }

            if (medicationsToInsert.Count > 0)
            {
                _context.Medications.AddRange(medicationsToInsert);
                await _context.SaveChangesAsync(cancellationToken);
                _analyticsService.InvalidateAnalyticsCache();
            }

            return (null, summary);
        }

        internal static string? ValidateFile(IFormFile? file, int maxFileSizeBytes)
        {
            if (file == null || file.Length == 0)
            {
                return "A CSV file is required.";
            }

            if (file.Length > maxFileSizeBytes)
            {
                return "File is too large. Maximum size is 5 MB.";
            }

            var fileName = file.FileName ?? string.Empty;
            var extension = Path.GetExtension(fileName);
            if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
                && !IsCsvContentType(file.ContentType))
            {
                return "Please upload a valid .csv file.";
            }

            return null;
        }

        private string? ValidateFile(IFormFile? file) => ValidateFile(file, MaxFileSizeBytes);

        private static bool IsCsvContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }

            return contentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase)
                || contentType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase)
                || contentType.Equals("application/csv", StringComparison.OrdinalIgnoreCase);
        }

        private static (string? Error, Dictionary<string, int>? ColumnIndexes) MapHeaders(string[] headerRow)
        {
            var columnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headerRow.Length; i++)
            {
                var header = headerRow[i].Trim();
                if (string.IsNullOrEmpty(header) || IgnoredHeaders.Contains(header))
                {
                    continue;
                }

                if (columnIndexes.ContainsKey(header))
                {
                    return ($"Duplicate column header '{header}'.", null);
                }

                columnIndexes[header] = i;
            }

            var missing = RequiredHeaders
                .Where(required => !columnIndexes.ContainsKey(required))
                .ToList();

            if (missing.Count > 0)
            {
                return ($"Missing required column(s): {string.Join(", ", missing)}.", null);
            }

            return (null, columnIndexes);
        }

        private static bool IsBlankRow(string[] cells) =>
            cells.All(cell => string.IsNullOrWhiteSpace(cell));

        private static bool TryParseRow(
            CsvDataRow row,
            Dictionary<string, int> columnIndexes,
            out ParsedMedicationRow? parsed,
            out List<MedicationImportRowErrorDto> errors)
        {
            parsed = null;
            var rowErrors = new List<MedicationImportRowErrorDto>();
            errors = rowErrors;

            string GetField(string header)
            {
                if (!columnIndexes.TryGetValue(header, out var index) || index >= row.Cells.Length)
                {
                    return string.Empty;
                }

                return row.Cells[index].Trim();
            }

            void AddError(string field, string? value, string reason)
            {
                rowErrors.Add(new MedicationImportRowErrorDto
                {
                    RowNumber = row.RowNumber,
                    Field = field,
                    Value = value,
                    Reason = reason
                });
            }

            var name = GetField("Name");
            if (name.Length < 3 || name.Length > 100)
            {
                AddError("Name", name, "Name is required and must be between 3 and 100 characters.");
            }

            var genericName = GetField("Generic Name");
            if (genericName.Length > 100)
            {
                AddError("Generic Name", genericName, "Generic name cannot exceed 100 characters.");
            }

            var category = GetField("Category");
            if (string.IsNullOrWhiteSpace(category))
            {
                AddError("Category", category, "Category is required.");
            }
            else if (category.Length > 50)
            {
                AddError("Category", category, "Category cannot exceed 50 characters.");
            }

            var manufacturer = GetField("Manufacturer");
            if (manufacturer.Length > 100)
            {
                AddError("Manufacturer", manufacturer, "Manufacturer cannot exceed 100 characters.");
            }

            var description = GetField("Description");
            if (description.Length > 500)
            {
                AddError("Description", description, "Description cannot exceed 500 characters.");
            }

            var priceRaw = GetField("Price");
            if (!TryParseDecimal(priceRaw, out var price) || price <= 0 || price > 10_000)
            {
                AddError("Price", priceRaw, "Price is required and must be greater than 0 and at most 10,000.");
            }

            var stockRaw = GetField("Stock Quantity");
            if (!TryParseInt(stockRaw, out var stockQuantity) || stockQuantity < 0 || stockQuantity > 10_000)
            {
                AddError("Stock Quantity", stockRaw, "Stock quantity is required and must be an integer between 0 and 10,000.");
            }

            var minimumStockRaw = GetField("Minimum Stock Level");
            if (!TryParseInt(minimumStockRaw, out var minimumStockLevel) || minimumStockLevel < 0 || minimumStockLevel > 10_000)
            {
                AddError("Minimum Stock Level", minimumStockRaw, "Minimum stock level is required and must be an integer between 0 and 10,000.");
            }

            var expiryRaw = GetField("Expiry Date");
            if (!TryParseDate(expiryRaw, out var expiryDate))
            {
                AddError("Expiry Date", expiryRaw, "Expiry date is required and must use YYYY-MM-DD format.");
            }
            else if (expiryDate.Date <= DateTime.Now.Date)
            {
                AddError("Expiry Date", expiryRaw, "Expiry date must be in the future.");
            }

            var batchNumber = GetField("Batch Number");
            if (batchNumber.Length > 50)
            {
                AddError("Batch Number", batchNumber, "Batch number cannot exceed 50 characters.");
            }

            var dosageForm = GetField("Dosage Form");
            if (dosageForm.Length > 50)
            {
                AddError("Dosage Form", dosageForm, "Dosage form cannot exceed 50 characters.");
            }

            var strength = GetField("Strength");
            if (strength.Length > 50)
            {
                AddError("Strength", strength, "Strength cannot exceed 50 characters.");
            }

            var requiresPrescriptionRaw = GetField("Requires Prescription");
            if (!TryParseYesNo(requiresPrescriptionRaw, out var requiresPrescription))
            {
                AddError("Requires Prescription", requiresPrescriptionRaw, "Requires Prescription is required and must be Yes or No.");
            }

            var activeRaw = GetField("Active");
            if (!TryParseYesNo(activeRaw, out var isActive))
            {
                AddError("Active", activeRaw, "Active is required and must be Yes or No.");
            }

            if (rowErrors.Count > 0)
            {
                return false;
            }

            parsed = new ParsedMedicationRow
            {
                Name = name,
                GenericName = string.IsNullOrWhiteSpace(genericName) ? null : genericName,
                Category = category,
                Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                Price = price,
                StockQuantity = stockQuantity,
                MinimumStockLevel = minimumStockLevel,
                ExpiryDate = expiryDate,
                BatchNumber = string.IsNullOrWhiteSpace(batchNumber) ? null : batchNumber,
                DosageForm = string.IsNullOrWhiteSpace(dosageForm) ? null : dosageForm,
                Strength = string.IsNullOrWhiteSpace(strength) ? null : strength,
                RequiresPrescription = requiresPrescription,
                IsActive = isActive
            };

            return true;
        }

        private static bool TryParseDecimal(string raw, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseInt(string raw, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            return raw.Trim() == value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryParseDate(string raw, out DateTime value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return DateTime.TryParseExact(
                raw,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out value);
        }

        private static bool TryParseYesNo(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (raw.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (raw.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return false;
        }

        private sealed record CsvDataRow(int RowNumber, string[] Cells);

        private sealed class ParsedMedicationRow
        {
            public required string Name { get; init; }

            public string? GenericName { get; init; }

            public required string Category { get; init; }

            public string? Manufacturer { get; init; }

            public string? Description { get; init; }

            public decimal Price { get; init; }

            public int StockQuantity { get; init; }

            public int MinimumStockLevel { get; init; }

            public DateTime ExpiryDate { get; init; }

            public string? BatchNumber { get; init; }

            public string? DosageForm { get; init; }

            public string? Strength { get; init; }

            public bool RequiresPrescription { get; init; }

            public bool IsActive { get; init; }

            public Medication ToEntity() => new()
            {
                Name = Name,
                GenericName = GenericName,
                Category = Category,
                Manufacturer = Manufacturer,
                Description = Description,
                Price = Price,
                StockQuantity = StockQuantity,
                MinimumStockLevel = MinimumStockLevel,
                ExpiryDate = ExpiryDate,
                BatchNumber = BatchNumber,
                DosageForm = DosageForm,
                Strength = Strength,
                RequiresPrescription = RequiresPrescription,
                IsActive = IsActive,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
