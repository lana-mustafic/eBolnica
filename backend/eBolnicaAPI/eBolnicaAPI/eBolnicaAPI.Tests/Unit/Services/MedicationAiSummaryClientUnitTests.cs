using System.Net;
using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAiSummaryClientUnitTests
    {
        [Fact]
        public async Task GenerateSummaryJsonAsync_WhenRequestTimesOut_ThrowsTimeoutUnavailableException()
        {
            var client = CreateClient(new DelayingHttpMessageHandler(TimeSpan.FromSeconds(5)), timeoutSeconds: 1);

            var exception = await Assert.ThrowsAsync<MedicationAiSummaryUnavailableException>(
                () => client.GenerateSummaryJsonAsync("system", "user name: Test"));

            Assert.Equal(StatusCodes.Status504GatewayTimeout, exception.StatusCode);
            Assert.Contains("timed out", exception.UserMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GenerateSummaryJsonAsync_WhenProviderReturns503_ThrowsGracefulUnavailableException()
        {
            var client = CreateClient(new StaticResponseHandler(HttpStatusCode.ServiceUnavailable, "provider down"));

            var exception = await Assert.ThrowsAsync<MedicationAiSummaryUnavailableException>(
                () => client.GenerateSummaryJsonAsync("system", "user name: Test"));

            Assert.Equal(StatusCodes.Status503ServiceUnavailable, exception.StatusCode);
            Assert.Contains("temporarily unavailable", exception.UserMessage, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("provider down", exception.UserMessage, StringComparison.OrdinalIgnoreCase);
        }

        private static MedicationAiSummaryClient CreateClient(
            HttpMessageHandler handler,
            int timeoutSeconds = 30)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.openai.com/v1/")
            };

            var settings = Options.Create(new MedicationAiSummarySettings
            {
                Enabled = true,
                Provider = "OpenAI",
                ApiKey = "test-key",
                Model = "gpt-4o-mini",
                TimeoutSeconds = timeoutSeconds
            });

            return new MedicationAiSummaryClient(
                httpClient,
                settings,
                Mock.Of<ILogger<MedicationAiSummaryClient>>());
        }

        private sealed class DelayingHttpMessageHandler : HttpMessageHandler
        {
            private readonly TimeSpan _delay;

            public DelayingHttpMessageHandler(TimeSpan delay)
            {
                _delay = delay;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                await Task.Delay(_delay, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        private sealed class StaticResponseHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _body;

            public StaticResponseHandler(HttpStatusCode statusCode, string body)
            {
                _statusCode = statusCode;
                _body = body;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_body)
                });
        }
    }
}
