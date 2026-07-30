using System.Net.Http.Json;
using System.Text.Json.Serialization;
using eBolnicaAPI.Models.Settings;
using Microsoft.Extensions.Options;

namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    public class MedicationAiSummaryClient : IMedicationAiSummaryClient
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<MedicationAiSummarySettings> _settings;

        public MedicationAiSummaryClient(
            HttpClient httpClient,
            IOptions<MedicationAiSummarySettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings;
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

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new MedicationAiSummaryUnavailableException("AI summary service timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new MedicationAiSummaryUnavailableException("AI summary service is temporarily unavailable.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new MedicationAiSummaryUnavailableException(
                    $"AI summary provider returned status {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new MedicationAiSummaryUnavailableException("AI summary provider returned an empty response.");
            }

            return content;
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
