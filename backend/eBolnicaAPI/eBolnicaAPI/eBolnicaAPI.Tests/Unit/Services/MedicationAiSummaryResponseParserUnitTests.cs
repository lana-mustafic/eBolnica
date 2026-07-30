using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAiSummaryResponseParserUnitTests
    {
        [Fact]
        public void Parse_ValidJson_ReturnsSummaryDto()
        {
            const string json = """
                {
                  "overview": "Aspirin is a painkiller tablet.",
                  "usageNotes": "Use as directed in the stored description.",
                  "stockExpiryAlert": "Stock is low and expiry is not soon.",
                  "prescriptionRequirement": "No prescription required."
                }
                """;

            var summary = MedicationAiSummaryResponseParser.Parse(json);

            Assert.Equal("Aspirin is a painkiller tablet.", summary.Overview);
            Assert.Equal("Use as directed in the stored description.", summary.UsageNotes);
            Assert.Equal("Stock is low and expiry is not soon.", summary.StockExpiryAlert);
            Assert.Equal("No prescription required.", summary.PrescriptionRequirement);
        }

        [Fact]
        public void Parse_MissingSection_ThrowsUnavailableException()
        {
            const string json = """
                {
                  "overview": "Only overview provided."
                }
                """;

            var exception = Assert.Throws<MedicationAiSummaryUnavailableException>(
                () => MedicationAiSummaryResponseParser.Parse(json));

            Assert.Contains("usageNotes", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
