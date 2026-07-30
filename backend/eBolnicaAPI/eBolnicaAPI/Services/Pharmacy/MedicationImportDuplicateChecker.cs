using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Case-insensitive medication name duplicate detection.
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

        /// <summary>
        /// Returns true when no other medication has the same trimmed, case-insensitive name.
        /// </summary>
        /// <param name="name">Medication name to check</param>
        /// <param name="excludeMedicationId">Optional medication ID to exclude (edit mode)</param>
        Task<bool> IsNameAvailableAsync(
            string name,
            int? excludeMedicationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns normalized medication names that appear more than once in the database.
        /// </summary>
        Task<IReadOnlyList<string>> FindExistingDuplicateNamesAsync(
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
            Medication.NormalizeNameValue(name);

        /// <summary>
        /// Finds normalized names that appear more than once (case-insensitive, trimmed).
        /// </summary>
        public static IReadOnlyList<string> FindDuplicateNormalizedNames(IEnumerable<string> names) =>
            names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .GroupBy(NormalizeName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

        public async Task<HashSet<string>> LoadExistingNormalizedNamesAsync(CancellationToken cancellationToken = default)
        {
            var names = await _context.Medications
                .AsNoTracking()
                .Select(m => m.NormalizedName)
                .ToListAsync(cancellationToken);

            return names
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

        public async Task<bool> IsNameAvailableAsync(
            string name,
            int? excludeMedicationId = null,
            CancellationToken cancellationToken = default)
        {
            var trimmedName = name?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(trimmedName))
            {
                throw new ArgumentException("Name is required.", nameof(name));
            }

            var normalizedName = NormalizeName(trimmedName);

            var nameTaken = await _context.Medications
                .AsNoTracking()
                .Where(m => excludeMedicationId == null || m.Id != excludeMedicationId.Value)
                .AnyAsync(m => m.NormalizedName == normalizedName, cancellationToken);

            return !nameTaken;
        }

        public async Task<IReadOnlyList<string>> FindExistingDuplicateNamesAsync(
            CancellationToken cancellationToken = default)
        {
            var names = await _context.Medications
                .AsNoTracking()
                .Select(m => m.NormalizedName)
                .ToListAsync(cancellationToken);

            return FindDuplicateNormalizedNames(names);
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
