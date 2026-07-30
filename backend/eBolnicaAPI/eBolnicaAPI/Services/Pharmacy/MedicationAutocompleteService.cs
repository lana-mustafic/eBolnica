using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Services.Pharmacy
{
    public interface IMedicationAutocompleteService
    {
        /// <summary>
        /// Returns lightweight medication suggestions for search autocomplete.
        /// </summary>
        /// <param name="query">Search query (minimum 2 trimmed characters)</param>
        /// <param name="limit">Maximum suggestions to return (1-10)</param>
        Task<IReadOnlyList<MedicationAutocompleteSuggestionDto>> GetSuggestionsAsync(
            string query,
            int limit = 10,
            CancellationToken cancellationToken = default);
    }

    public class MedicationAutocompleteService : IMedicationAutocompleteService
    {
        public const int MinQueryLength = 2;
        public const int MaxSuggestions = 10;

        private readonly AppDbContext _context;

        public MedicationAutocompleteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<MedicationAutocompleteSuggestionDto>> GetSuggestionsAsync(
            string query,
            int limit = MaxSuggestions,
            CancellationToken cancellationToken = default)
        {
            var trimmed = query?.Trim() ?? string.Empty;

            if (trimmed.Length < MinQueryLength)
            {
                return Array.Empty<MedicationAutocompleteSuggestionDto>();
            }

            var cappedLimit = Math.Clamp(limit, 1, MaxSuggestions);
            var searchTerm = trimmed.ToLower();

            return await _context.Medications
                .AsNoTracking()
                .Where(m => m.IsActive)
                .Where(m =>
                    m.Name.ToLower().Contains(searchTerm) ||
                    (m.GenericName != null && m.GenericName.ToLower().Contains(searchTerm)) ||
                    (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(searchTerm)))
                .OrderBy(m => m.Name)
                .Take(cappedLimit)
                .Select(m => new MedicationAutocompleteSuggestionDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Category = m.Category,
                    GenericName = m.GenericName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
