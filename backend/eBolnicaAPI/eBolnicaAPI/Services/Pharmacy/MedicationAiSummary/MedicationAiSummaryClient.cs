using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eBolnicaAPI.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    public class MedicationAiSummaryClient : IMedicationAiSummaryClient
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<MedicationAiSummarySettings> _settings;
        private readonly ILogger<MedicationAiSummaryClient> _logger;

        public MedicationAiSummaryClient(
            HttpClient httpClient,
            IOptions<MedicationAiSummarySettings> settings,
            ILogger<MedicationAiSummaryClient> logger)
        {
            _httpClient = httpClient;
            _settings = settings;
            _logger = logger;
        }

        public async Task<string> GenerateSummaryJsonAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            using var request = MedicationAiSummaryLlmRequestBuilder.CreateChatCompletionRequest(
                _settings.Value,
                systemPrompt,
                userPrompt);

            var timeout = TimeSpan.FromSeconds(Math.Max(5, _settings.Value.TimeoutSeconds));
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, timeoutCts.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "AI summary LLM request timed out after {TimeoutSeconds}s", timeout.TotalSeconds);
                throw MedicationAiSummaryUnavailableException.Timeout(ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "AI summary LLM request failed due to a network error");
                throw MedicationAiSummaryUnavailableException.ServiceUnavailable(
                    "AI summary LLM network request failed.",
                    ex);
            }

            if (IsTimeoutStatusCode(response.StatusCode))
            {
                _logger.LogWarning(
                    "AI summary provider returned timeout status {StatusCode}",
                    (int)response.StatusCode);
                throw MedicationAiSummaryUnavailableException.Timeout();
            }

            if (!response.IsSuccessStatusCode)
            {
                var providerError = await TryReadProviderErrorAsync(response, timeoutCts.Token);
                _logger.LogWarning(
                    "AI summary provider returned status {StatusCode}. Details: {ProviderError}",
                    (int)response.StatusCode,
                    MedicationAiSummarySecretSanitizer.SanitizeForLogs(providerError) is { Length: > 0 } sanitized
                        ? sanitized
                        : "(none)");

                throw MedicationAiSummaryUnavailableException.ServiceUnavailable(
                    $"AI summary provider returned status {(int)response.StatusCode}.");
            }

            ChatCompletionResponse? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(timeoutCts.Token);
            }
            catch (Exception ex) when (ex is JsonException or OperationCanceledException or NotSupportedException)
            {
                _logger.LogWarning(ex, "Failed to deserialize AI summary provider response");
                throw MedicationAiSummaryUnavailableException.InvalidProviderResponse(
                    "AI summary provider response could not be parsed.");
            }

            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw MedicationAiSummaryUnavailableException.InvalidProviderResponse(
                    "AI summary provider returned empty content.");
            }

            return content;
        }

        private static bool IsTimeoutStatusCode(HttpStatusCode statusCode) =>
            statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout;

        private static async Task<string?> TryReadProviderErrorAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return string.IsNullOrWhiteSpace(body) ? null : body.Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private sealed class ChatCompletionResponse
        {
            [JsonPropertyName("choices")]
            public ChatChoice[]? Choices { get; set; }
        }

        private sealed class ChatChoice
        {
            [JsonPropertyName("message")]
            public ChatMessageResponse? Message { get; set; }
        }

        private sealed class ChatMessageResponse
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
