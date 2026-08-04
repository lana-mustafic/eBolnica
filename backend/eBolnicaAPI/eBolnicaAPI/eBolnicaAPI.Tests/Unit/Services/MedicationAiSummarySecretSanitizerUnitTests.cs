using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAiSummarySecretSanitizerUnitTests
    {
        [Fact]
        public void SanitizeForLogs_RedactsBearerAndApiKeyPatterns()
        {
            const string raw =
                "Authorization failed Bearer sk-test-secret-key and api-key=super-secret-value";

            var sanitized = MedicationAiSummarySecretSanitizer.SanitizeForLogs(raw);

            Assert.DoesNotContain("sk-test-secret-key", sanitized, StringComparison.Ordinal);
            Assert.DoesNotContain("super-secret-value", sanitized, StringComparison.Ordinal);
            Assert.Contains("Bearer ***", sanitized, StringComparison.Ordinal);
            Assert.Contains("api-key=***", sanitized, StringComparison.Ordinal);
        }

        [Fact]
        public void SanitizeForLogs_RedactsStandaloneOpenAiKey()
        {
            const string raw = "Invalid key sk-test-secret-key in request";

            var sanitized = MedicationAiSummarySecretSanitizer.SanitizeForLogs(raw);

            Assert.DoesNotContain("sk-test-secret-key", sanitized, StringComparison.Ordinal);
            Assert.Contains("sk-***", sanitized, StringComparison.Ordinal);
        }

        [Fact]
        public void CreateChatCompletionRequest_DoesNotEmbedApiKeyInRequestBody()
        {
            using var request = MedicationAiSummaryLlmRequestBuilder.CreateChatCompletionRequest(
                new eBolnicaAPI.Models.Settings.MedicationAiSummarySettings
                {
                    Enabled = true,
                    Provider = "OpenAI",
                    ApiKey = "sk-test-secret-key",
                    Model = "gpt-4o-mini"
                },
                "system",
                "user name: Test");

            var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

            Assert.DoesNotContain("sk-test-secret-key", body, StringComparison.Ordinal);
            Assert.DoesNotContain("ApiKey", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api-key", body, StringComparison.OrdinalIgnoreCase);
        }
    }
}
