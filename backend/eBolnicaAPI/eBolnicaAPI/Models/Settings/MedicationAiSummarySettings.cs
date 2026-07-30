using System.Text.Json.Serialization;

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
        /// LLM provider: OpenAI or AzureOpenAI.
        /// </summary>
        public string Provider { get; set; } = "OpenAI";

        /// <summary>
        /// Provider API key. Server-side only — never expose to clients.
        /// Set via MedicationAiSummary__ApiKey environment variable or user secrets.
        /// </summary>
        [JsonIgnore]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// OpenAI API base URL.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

        /// <summary>
        /// OpenAI model id (also used as Azure deployment fallback when DeploymentName is empty).
        /// </summary>
        public string Model { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// Azure OpenAI resource endpoint, e.g. https://my-resource.openai.azure.com/
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Azure OpenAI deployment name.
        /// </summary>
        public string DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// Azure OpenAI REST API version query parameter.
        /// </summary>
        public string ApiVersion { get; set; } = "2024-08-01-preview";

        public int TimeoutSeconds { get; set; } = 30;
    }
}
