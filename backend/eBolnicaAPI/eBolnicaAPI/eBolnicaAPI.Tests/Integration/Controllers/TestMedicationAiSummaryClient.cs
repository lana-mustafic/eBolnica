using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;

namespace eBolnicaAPI.Tests.Integration.Controllers
{
    internal sealed class TestMedicationAiSummaryClient : IMedicationAiSummaryClient
    {
        public Task<string> GenerateSummaryJsonAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userPrompt) || !userPrompt.Contains("name:", StringComparison.Ordinal))
            {
                throw MedicationAiSummaryUnavailableException.InvalidProviderResponse(
                    "Test AI client received an invalid prompt.");
            }

            return Task.FromResult(
                """
                {
                  "overview": "Test overview",
                  "usageNotes": "Test usage notes",
                  "stockExpiryAlert": "Test stock alert",
                  "prescriptionRequirement": "Test prescription info"
                }
                """);
        }
    }
}
