using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAiSummaryPromptBuilderUnitTests
    {
        [Fact]
        public void BuildUserPrompt_IncludesStoredMedicationFieldsOnly()
        {
            var medication = new Medication
            {
                Name = "Aspirin",
                GenericName = "Acetylsalicylic acid",
                Category = "painkiller",
                DosageForm = "tablet",
                Strength = "500mg",
                Description = "Pain relief medication",
                StockQuantity = 5,
                MinimumStockLevel = 20,
                ExpiryDate = new DateTime(2026, 12, 31),
                RequiresPrescription = false,
                IsActive = true
            };

            var prompt = MedicationAiSummaryPromptBuilder.BuildUserPrompt(medication);

            Assert.Contains("name: Aspirin", prompt, StringComparison.Ordinal);
            Assert.Contains("genericName: Acetylsalicylic acid", prompt, StringComparison.Ordinal);
            Assert.Contains("category: painkiller", prompt, StringComparison.Ordinal);
            Assert.Contains("dosage: tablet 500mg", prompt, StringComparison.Ordinal);
            Assert.Contains("description: Pain relief medication", prompt, StringComparison.Ordinal);
            Assert.Contains("stockQuantity: 5", prompt, StringComparison.Ordinal);
            Assert.Contains("minimumStockLevel: 20", prompt, StringComparison.Ordinal);
            Assert.Contains("stockStatus: low stock", prompt, StringComparison.Ordinal);
            Assert.Contains("expiryDate: 2026-12-31", prompt, StringComparison.Ordinal);
            Assert.Contains("requiresPrescription: False", prompt, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(0, 10, "out of stock")]
        [InlineData(5, 10, "low stock")]
        [InlineData(25, 10, "normal stock")]
        public void BuildStockStatus_ReturnsExpectedLabel(int stock, int minimum, string expected)
        {
            Assert.Equal(expected, MedicationAiSummaryPromptBuilder.BuildStockStatus(stock, minimum));
        }

        [Fact]
        public void BuildDosageLabel_ReturnsNotRecordedWhenMissing()
        {
            Assert.Equal("Not recorded", MedicationAiSummaryPromptBuilder.BuildDosageLabel(null, null));
        }
    }
}
