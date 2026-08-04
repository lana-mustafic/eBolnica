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
        public const int MinQueryLength = MedicationAutocompleteLimits.MinQueryLength;
        public const int MaxSuggestions = MedicationAutocompleteLimits.MaxSuggestions;

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

            if (!MedicationAutocompleteLimits.IsQueryLongEnough(trimmed))
            {
                return Array.Empty<MedicationAutocompleteSuggestionDto>();
            }

            var cappedLimit = MedicationAutocompleteLimits.CapSuggestionLimit(limit);

            return await MedicationAutocompleteQuery
                .GetSuggestions(_context.Medications, trimmed, cappedLimit)
                .ToListAsync(cancellationToken);
        }
    }
}
