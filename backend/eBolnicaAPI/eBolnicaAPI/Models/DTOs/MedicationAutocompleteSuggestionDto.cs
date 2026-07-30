namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Lightweight medication suggestion for search autocomplete.
    /// </summary>
    public class MedicationAutocompleteSuggestionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? GenericName { get; set; }
    }
}
