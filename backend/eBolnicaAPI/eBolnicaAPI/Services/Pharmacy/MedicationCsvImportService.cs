using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Parses and imports medication CSV files (multipart upload).
    /// </summary>
    public class MedicationCsvImportService : IMedicationCsvImportService
    {
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

                if (!MedicationCsvRowValidator.TryValidateRow(
                        row.RowNumber,
                        row.Cells,
                        headerMapResult.ColumnIndexes!,
                        out var dto,
                        out var rowErrors))
                {
                    summary.Errors.AddRange(rowErrors);
                    summary.FailureCount++;
                    continue;
                }

                if (reservedNames.Contains(dto!.Name))
                {
                    summary.Errors.Add(new MedicationImportRowErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Field = "Name",
                        Value = dto.Name,
                        Reason = "A medication with this name already exists."
                    });
                    summary.FailureCount++;
                    continue;
                }

                reservedNames.Add(dto.Name);
                medicationsToInsert.Add(ToEntity(dto));
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

            var missing = MedicationCsvRowValidator.RequiredHeaders
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

        private static Medication ToEntity(MedicationCreateDto dto) => new()
        {
            Name = dto.Name,
            GenericName = dto.GenericName,
            Category = dto.Category,
            Manufacturer = dto.Manufacturer,
            Description = dto.Description,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            MinimumStockLevel = dto.MinimumStockLevel,
            ExpiryDate = dto.ExpiryDate,
            BatchNumber = dto.BatchNumber,
            DosageForm = dto.DosageForm,
            Strength = dto.Strength,
            RequiresPrescription = dto.RequiresPrescription,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        private sealed record CsvDataRow(int RowNumber, string[] Cells);
    }
}
