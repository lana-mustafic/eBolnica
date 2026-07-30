namespace eBolnicaAPI.Models.Settings
{
    /// <summary>
    /// Configuration for medication AI summary generation.
    /// Store <see cref="ApiKey"/> in environment variables or user secrets, not in source control.
    /// </summary>
    public class MedicationAiSummarySettings
    {
        public const string SectionName = "MedicationAiSummary";

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Provider API key (e.g. OpenAI). Leave empty to disable generation.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

        public string Model { get; set; } = "gpt-4o-mini";

        public int TimeoutSeconds { get; set; } = 30;
    }
}
