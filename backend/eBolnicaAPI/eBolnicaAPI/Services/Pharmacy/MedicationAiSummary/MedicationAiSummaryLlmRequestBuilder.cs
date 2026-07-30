using System.Net.Http.Json;
using eBolnicaAPI.Models.Settings;

namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    public static class MedicationAiSummaryLlmRequestBuilder
    {
        public static void EnsureConfigured(MedicationAiSummarySettings settings)
        {
            if (!settings.Enabled)
            {
                throw new MedicationAiSummaryUnavailableException("AI summary service is disabled.");
            }

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new MedicationAiSummaryUnavailableException("AI summary service is not configured.");
            }

            var provider = MedicationAiSummaryLlmProvider.Normalize(settings.Provider);
            if (!MedicationAiSummaryLlmProvider.IsSupported(provider))
            {
                throw new MedicationAiSummaryUnavailableException(
                    $"AI summary provider '{settings.Provider}' is not supported.");
            }

            if (provider == MedicationAiSummaryLlmProvider.OpenAi)
            {
                if (string.IsNullOrWhiteSpace(settings.Model))
                {
                    throw new MedicationAiSummaryUnavailableException("OpenAI model is not configured.");
                }

                if (string.IsNullOrWhiteSpace(settings.BaseUrl))
                {
                    throw new MedicationAiSummaryUnavailableException("OpenAI base URL is not configured.");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(settings.Endpoint))
            {
                throw new MedicationAiSummaryUnavailableException("Azure OpenAI endpoint is not configured.");
            }

            if (string.IsNullOrWhiteSpace(ResolveDeploymentName(settings)))
            {
                throw new MedicationAiSummaryUnavailableException("Azure OpenAI deployment name is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.ApiVersion))
            {
                throw new MedicationAiSummaryUnavailableException("Azure OpenAI API version is not configured.");
            }
        }

        public static Uri ResolveHttpClientBaseAddress(MedicationAiSummarySettings settings)
        {
            var provider = MedicationAiSummaryLlmProvider.Normalize(settings.Provider);
            if (provider == MedicationAiSummaryLlmProvider.AzureOpenAi &&
                !string.IsNullOrWhiteSpace(settings.Endpoint))
            {
                return new Uri(settings.Endpoint.TrimEnd('/') + "/");
            }

            var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
                ? "https://api.openai.com/v1/"
                : settings.BaseUrl;

            return new Uri(baseUrl.TrimEnd('/') + "/");
        }

        public static HttpRequestMessage CreateChatCompletionRequest(
            MedicationAiSummarySettings settings,
            string systemPrompt,
            string userPrompt)
        {
            EnsureConfigured(settings);

            var provider = MedicationAiSummaryLlmProvider.Normalize(settings.Provider);
            var requestUri = provider == MedicationAiSummaryLlmProvider.AzureOpenAi
                ? BuildAzureRequestUri(settings)
                : "chat/completions";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            ConfigureAuthentication(request, settings);

            request.Content = JsonContent.Create(new ChatCompletionRequest
            {
                Model = provider == MedicationAiSummaryLlmProvider.OpenAi
                    ? settings.Model.Trim()
                    : null,
                ResponseFormat = new ResponseFormat { Type = "json_object" },
                Messages =
                [
                    new ChatMessage { Role = "system", Content = systemPrompt },
                    new ChatMessage { Role = "user", Content = userPrompt }
                ]
            });

            return request;
        }

        internal static string ResolveDeploymentName(MedicationAiSummarySettings settings) =>
            !string.IsNullOrWhiteSpace(settings.DeploymentName)
                ? settings.DeploymentName.Trim()
                : settings.Model?.Trim() ?? string.Empty;

        private static string BuildAzureRequestUri(MedicationAiSummarySettings settings)
        {
            var deployment = Uri.EscapeDataString(ResolveDeploymentName(settings));
            var apiVersion = Uri.EscapeDataString(settings.ApiVersion.Trim());
            return $"openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";
        }

        private static void ConfigureAuthentication(HttpRequestMessage request, MedicationAiSummarySettings settings)
        {
            var provider = MedicationAiSummaryLlmProvider.Normalize(settings.Provider);

            if (provider == MedicationAiSummaryLlmProvider.AzureOpenAi)
            {
                request.Headers.Add("api-key", settings.ApiKey.Trim());
                return;
            }

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
        }

        internal sealed class ChatCompletionRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("model")]
            [System.Text.Json.Serialization.JsonIgnore(
                Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string? Model { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("messages")]
            public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

            [System.Text.Json.Serialization.JsonPropertyName("response_format")]
            public ResponseFormat ResponseFormat { get; set; } = new();
        }

        internal sealed class ChatMessage
        {
            [System.Text.Json.Serialization.JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        internal sealed class ResponseFormat
        {
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string Type { get; set; } = "json_object";
        }
    }
}
