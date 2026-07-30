using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAiSummaryPromptBuilderUnitTests
    {
        [Fact]
        public void ReadEntityFields_UsesOnlyWhitelistedMedicationScalars()
        {
            var medication = CreateSampleMedication();

            var fields = MedicationAiSummaryPromptBuilder.ReadEntityFields(medication);

            Assert.Equal(MedicationAiSummaryPromptBuilder.EntityFieldKeys, fields.Keys.ToArray());
            Assert.Equal("Aspirin", fields["name"]);
            Assert.Equal("painkiller", fields["category"]);
            Assert.Equal("tablet", fields["dosageForm"]);
            Assert.Equal("500mg", fields["strength"]);
            Assert.Equal("Pain relief medication", fields["description"]);
            Assert.Equal("5", fields["stockQuantity"]);
            Assert.Equal("20", fields["minimumStockLevel"]);
            Assert.Equal("2026-12-31", fields["expiryDate"]);
            Assert.Equal("False", fields["requiresPrescription"]);
        }

        [Fact]
        public void BuildUserPrompt_IncludesOnlyMedicationEntityFields()
        {
            var medication = CreateSampleMedication();
            medication.Manufacturer = "PharmaCorp";
            medication.Price = 99.99m;
            medication.BatchNumber = "BATCH-1";
            medication.NormalizedName = "aspirin";
            medication.ImageUrl = "/uploads/aspirin.jpg";
            medication.IsActive = false;

            var prompt = MedicationAiSummaryPromptBuilder.BuildUserPrompt(medication);
            var fieldLines = prompt
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.Contains(':'))
                .Select(line => line.Split(':', 2)[0])
                .Where(key => MedicationAiSummaryPromptBuilder.EntityFieldKeys.Contains(key))
                .ToArray();

            Assert.Equal(MedicationAiSummaryPromptBuilder.EntityFieldKeys, fieldLines);
            Assert.Contains("name: Aspirin", prompt, StringComparison.Ordinal);
            Assert.Contains("category: painkiller", prompt, StringComparison.Ordinal);
            Assert.Contains("dosageForm: tablet", prompt, StringComparison.Ordinal);
            Assert.Contains("strength: 500mg", prompt, StringComparison.Ordinal);
            Assert.Contains("description: Pain relief medication", prompt, StringComparison.Ordinal);
            Assert.Contains("stockQuantity: 5", prompt, StringComparison.Ordinal);
            Assert.Contains("minimumStockLevel: 20", prompt, StringComparison.Ordinal);
            Assert.Contains("expiryDate: 2026-12-31", prompt, StringComparison.Ordinal);
            Assert.Contains("requiresPrescription: False", prompt, StringComparison.Ordinal);

            Assert.DoesNotContain("manufacturer:", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("price:", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("batchNumber:", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("normalizedName:", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("imageUrl:", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("isActive:", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stockStatus:", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("expiryStatus:", prompt, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("genericName:", prompt, StringComparison.OrdinalIgnoreCase);
        }

        private static Medication CreateSampleMedication() =>
            new()
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
    }
}
