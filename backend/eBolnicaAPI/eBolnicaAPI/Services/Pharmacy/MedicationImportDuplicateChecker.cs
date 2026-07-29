using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Case-insensitive medication name duplicate detection for CSV import.
    /// </summary>
    public interface IMedicationImportDuplicateChecker
    {
        Task<HashSet<string>> LoadExistingNormalizedNamesAsync(CancellationToken cancellationToken = default);

        MedicationImportRowErrorDto? TryRegisterName(
            string name,
            int rowNumber,
            HashSet<string> existingNormalizedNames,
            HashSet<string> importNormalizedNames);

        Task<IReadOnlyList<string>> FindConflictingNamesAsync(
            IEnumerable<string> names,
            CancellationToken cancellationToken = default);
    }

    public class MedicationImportDuplicateChecker : IMedicationImportDuplicateChecker
    {
        private readonly AppDbContext _context;

        public MedicationImportDuplicateChecker(AppDbContext context)
        {
            _context = context;
        }

        public static string NormalizeName(string name) =>
            name.Trim().ToLowerInvariant();

        public async Task<HashSet<string>> LoadExistingNormalizedNamesAsync(CancellationToken cancellationToken = default)
        {
            var names = await _context.Medications
                .AsNoTracking()
                .Select(m => m.Name)
                .ToListAsync(cancellationToken);

            return names
                .Select(NormalizeName)
                .ToHashSet(StringComparer.Ordinal);
        }

        public MedicationImportRowErrorDto? TryRegisterName(
            string name,
            int rowNumber,
            HashSet<string> existingNormalizedNames,
            HashSet<string> importNormalizedNames)
        {
            var normalized = NormalizeName(name);

            if (importNormalizedNames.Contains(normalized))
            {
                return CreateRowError(
                    rowNumber,
                    name,
                    "Duplicate name in this import file.");
            }

            if (existingNormalizedNames.Contains(normalized))
            {
                return CreateRowError(
                    rowNumber,
                    name,
                    "A medication with this name already exists.");
            }

            importNormalizedNames.Add(normalized);
            return null;
        }

        public async Task<IReadOnlyList<string>> FindConflictingNamesAsync(
            IEnumerable<string> names,
            CancellationToken cancellationToken = default)
        {
            var normalizedBatch = names
                .Select(NormalizeName)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (normalizedBatch.Count == 0)
            {
                return Array.Empty<string>();
            }

            var existingNames = await LoadExistingNormalizedNamesAsync(cancellationToken);
            return normalizedBatch
                .Where(existingNames.Contains)
                .ToList();
        }

        private static MedicationImportRowErrorDto CreateRowError(int rowNumber, string name, string reason) =>
            new()
            {
                RowNumber = rowNumber,
                Field = "Name",
                Value = name,
                Reason = reason
            };
    }
}
