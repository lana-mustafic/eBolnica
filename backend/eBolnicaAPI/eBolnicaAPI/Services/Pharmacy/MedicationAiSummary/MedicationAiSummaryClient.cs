using System.Net.Http.Headers;
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
            var settings = _settings.Value;

            if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new MedicationAiSummaryUnavailableException("AI summary service is not configured.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            request.Content = JsonContent.Create(new ChatCompletionRequest
            {
                Model = settings.Model,
                ResponseFormat = new ResponseFormat { Type = "json_object" },
                Messages =
                [
                    new ChatMessage { Role = "system", Content = systemPrompt },
                    new ChatMessage { Role = "user", Content = userPrompt }
                ]
            });

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

        private sealed class ChatCompletionRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

            [JsonPropertyName("response_format")]
            public ResponseFormat ResponseFormat { get; set; } = new();
        }

        private sealed class ChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class ResponseFormat
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "json_object";
        }

        private sealed class ChatCompletionResponse
        {
            [JsonPropertyName("choices")]
            public ChatChoice[]? Choices { get; set; }
        }

        private sealed class ChatChoice
        {
            [JsonPropertyName("message")]
            public ChatMessage? Message { get; set; }
        }
    }
}
