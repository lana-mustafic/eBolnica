namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    public static class MedicationAiSummaryLlmProvider
    {
        public const string OpenAi = "OpenAI";
        public const string AzureOpenAi = "AzureOpenAI";

        public static string Normalize(string? provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return OpenAi;
            }

            return provider.Trim() switch
            {
                "openai" => OpenAi,
                "azureopenai" or "azure" or "azure_openai" => AzureOpenAi,
                _ when provider.Equals(OpenAi, StringComparison.OrdinalIgnoreCase) => OpenAi,
                _ when provider.Equals(AzureOpenAi, StringComparison.OrdinalIgnoreCase) => AzureOpenAi,
                _ => provider.Trim()
            };
        }

        public static bool IsSupported(string? provider)
        {
            var normalized = Normalize(provider);
            return normalized is OpenAi or AzureOpenAi;
        }
    }
}
