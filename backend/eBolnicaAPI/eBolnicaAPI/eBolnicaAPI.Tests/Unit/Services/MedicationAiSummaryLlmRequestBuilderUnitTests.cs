using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAiSummaryLlmRequestBuilderUnitTests
    {
        [Fact]
        public void CreateChatCompletionRequest_OpenAi_UsesBearerAuthAndModel()
        {
            var settings = new MedicationAiSummarySettings
            {
                Enabled = true,
                Provider = "OpenAI",
                ApiKey = "openai-key",
                BaseUrl = "https://api.openai.com/v1/",
                Model = "gpt-4o-mini"
            };

            using var request = MedicationAiSummaryLlmRequestBuilder.CreateChatCompletionRequest(
                settings,
                "system",
                "user");

            Assert.Equal("chat/completions", request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("openai-key", request.Headers.Authorization?.Parameter);
            Assert.False(request.Headers.Contains("api-key"));

            var body = ReadRequestBody(request);
            Assert.Contains("\"model\":\"gpt-4o-mini\"", body, StringComparison.Ordinal);
        }

        [Fact]
        public void CreateChatCompletionRequest_AzureOpenAi_UsesApiKeyHeaderAndDeploymentPath()
        {
            var settings = new MedicationAiSummarySettings
            {
                Enabled = true,
                Provider = "AzureOpenAI",
                ApiKey = "azure-key",
                Endpoint = "https://my-resource.openai.azure.com/",
                DeploymentName = "gpt-4o-mini",
                ApiVersion = "2024-08-01-preview"
            };

            using var request = MedicationAiSummaryLlmRequestBuilder.CreateChatCompletionRequest(
                settings,
                "system",
                "user");

            Assert.Equal(
                "openai/deployments/gpt-4o-mini/chat/completions?api-version=2024-08-01-preview",
                request.RequestUri?.ToString());
            Assert.Null(request.Headers.Authorization);
            Assert.Equal("azure-key", request.Headers.GetValues("api-key").Single());

            var body = ReadRequestBody(request);
            Assert.DoesNotContain("\"model\":", body, StringComparison.Ordinal);
        }

        [Fact]
        public void ResolveHttpClientBaseAddress_SelectsProviderSpecificHost()
        {
            var openAiAddress = MedicationAiSummaryLlmRequestBuilder.ResolveHttpClientBaseAddress(
                new MedicationAiSummarySettings
                {
                    Provider = "OpenAI",
                    BaseUrl = "https://api.openai.com/v1/"
                });

            var azureAddress = MedicationAiSummaryLlmRequestBuilder.ResolveHttpClientBaseAddress(
                new MedicationAiSummarySettings
                {
                    Provider = "AzureOpenAI",
                    Endpoint = "https://my-resource.openai.azure.com/"
                });

            Assert.Equal("https://api.openai.com/v1/", openAiAddress.ToString());
            Assert.Equal("https://my-resource.openai.azure.com/", azureAddress.ToString());
        }

        [Fact]
        public void EnsureConfigured_AzureOpenAi_RequiresEndpointAndDeployment()
        {
            var exception = Assert.Throws<MedicationAiSummaryUnavailableException>(() =>
                MedicationAiSummaryLlmRequestBuilder.EnsureConfigured(new MedicationAiSummarySettings
                {
                    Enabled = true,
                    Provider = "AzureOpenAI",
                    ApiKey = "azure-key",
                    Model = "gpt-4o-mini"
                }));

            Assert.Contains("endpoint", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadRequestBody(HttpRequestMessage request)
        {
            Assert.NotNull(request.Content);
            return request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
    }
}
