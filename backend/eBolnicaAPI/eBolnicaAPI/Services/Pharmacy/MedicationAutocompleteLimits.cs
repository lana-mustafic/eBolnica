namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Autocomplete request limits shared by API and query layers.
    /// </summary>
    public static class MedicationAutocompleteLimits
    {
        public const int MinQueryLength = 2;

        public const int MaxSuggestions = 10;

        public static int CapSuggestionLimit(int limit) =>
            Math.Clamp(limit, 1, MaxSuggestions);

        public static bool IsQueryLongEnough(string? query) =>
            (query?.Trim().Length ?? 0) >= MinQueryLength;
    }
}
