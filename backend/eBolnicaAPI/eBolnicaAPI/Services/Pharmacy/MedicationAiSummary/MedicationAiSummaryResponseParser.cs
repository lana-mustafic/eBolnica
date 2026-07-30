using System.Text.Json;
using eBolnicaAPI.Models.DTOs;

namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    public static class MedicationAiSummaryResponseParser
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static MedicationAiSummaryDto Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw MedicationAiSummaryUnavailableException.InvalidProviderResponse("AI summary response was empty.");
            }

            MedicationAiSummaryDto? summary;
            try
            {
                summary = JsonSerializer.Deserialize<MedicationAiSummaryDto>(json, JsonOptions);
            }
            catch (JsonException)
            {
                throw MedicationAiSummaryUnavailableException.InvalidProviderResponse(
                    "AI summary response was not valid JSON.");
            }

            if (summary == null)
            {
                throw MedicationAiSummaryUnavailableException.InvalidProviderResponse("AI summary response was empty.");
            }

            summary.Overview = RequireSection(summary.Overview, "overview");
            summary.UsageNotes = RequireSection(summary.UsageNotes, "usageNotes");
            summary.StockExpiryAlert = RequireSection(summary.StockExpiryAlert, "stockExpiryAlert");
            summary.PrescriptionRequirement = RequireSection(summary.PrescriptionRequirement, "prescriptionRequirement");

            return summary;
        }

        private static string RequireSection(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw MedicationAiSummaryUnavailableException.InvalidProviderResponse(
                    $"AI summary response missing required field: {fieldName}.");
            }

            return value.Trim();
        }
    }
}
