using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Lightweight autocomplete query over medication Name, GenericName, and Manufacturer.
    /// </summary>
    public static class MedicationAutocompleteQuery
    {
        /// <summary>
        /// Builds a trimmed, lower-case search term for autocomplete matching.
        /// </summary>
        public static string NormalizeSearchTerm(string query) =>
            Medication.NormalizeNameValue(query ?? string.Empty);

        /// <summary>
        /// Returns up to <paramref name="limit"/> active medication suggestions matching the search term.
        /// Projects only fields required by the autocomplete dropdown.
        /// </summary>
        public static IQueryable<MedicationAutocompleteSuggestionDto> GetSuggestions(
            IQueryable<Medication> medications,
            string searchQuery,
            int limit)
        {
            var normalizedSearch = NormalizeSearchTerm(searchQuery);
            var cappedLimit = MedicationAutocompleteLimits.CapSuggestionLimit(limit);

            return medications
                .AsNoTracking()
                .Where(m => m.IsActive)
                .Where(m =>
                    m.NormalizedName.Contains(normalizedSearch) ||
                    (m.GenericName != null && m.GenericName.ToLower().Contains(normalizedSearch)) ||
                    (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(normalizedSearch)))
                .OrderBy(m => m.Name)
                .Take(cappedLimit)
                .Select(m => new MedicationAutocompleteSuggestionDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Category = m.Category,
                    Manufacturer = m.Manufacturer
                });
        }
    }
}
