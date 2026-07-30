using eBolnicaAPI.Services.Pharmacy;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    /// <summary>
    /// Guards seeded medication name lists against case-insensitive duplicates.
    /// </summary>
    public class MedicationSeedDataDuplicateTests
    {
        /// <summary>
        /// Names from SeedDatabase migration (Medications InsertData).
        /// </summary>
        public static readonly string[] MigrationSeedMedicationNames =
        {
            "Paracetamol",
            "Ibuprofen",
            "Amoxicillin",
            "Aspirin",
            "Cetirizine",
            "Omeprazole",
            "Metformin",
            "Loratadine",
            "Azithromycin",
            "Vitamin D3",
            "Ciprofloxacin",
            "Diclofenac",
            "Fexofenadine",
            "Calcium Carbonate",
            "Atorvastatin"
        };

        /// <summary>
        /// Names from integration/unit pharmacy test seed helpers.
        /// </summary>
        public static readonly string[] IntegrationTestSeedMedicationNames =
        {
            "Penicillin",
            "Amoxicillin",
            "Aspirin",
            "Ibuprofen",
            "Discontinued Drug",
            "EmptyStock Med",
            "Critical Stock Med",
            "Expiry Good Med",
            "Expiry Warning Med",
            "Expiry Critical Med",
            "Expiry Expired Med"
        };

        [Fact]
        public void MigrationSeedMedicationNames_HaveNoCaseInsensitiveDuplicates()
        {
            var duplicates = MedicationImportDuplicateChecker.FindDuplicateNormalizedNames(
                MigrationSeedMedicationNames);

            Assert.Empty(duplicates);
        }

        [Fact]
        public void IntegrationTestSeedMedicationNames_HaveNoCaseInsensitiveDuplicates()
        {
            var duplicates = MedicationImportDuplicateChecker.FindDuplicateNormalizedNames(
                IntegrationTestSeedMedicationNames);

            Assert.Empty(duplicates);
        }

        [Fact]
        public void FindDuplicateNormalizedNames_DetectsCaseAndWhitespaceDuplicates()
        {
            var duplicates = MedicationImportDuplicateChecker.FindDuplicateNormalizedNames(new[]
            {
                "Aspirin",
                "  aspirin  ",
                "Ibuprofen",
                "IBUPROFEN"
            });

            Assert.Equal(2, duplicates.Count);
            Assert.Contains("aspirin", duplicates);
            Assert.Contains("ibuprofen", duplicates);
        }
    }
}
