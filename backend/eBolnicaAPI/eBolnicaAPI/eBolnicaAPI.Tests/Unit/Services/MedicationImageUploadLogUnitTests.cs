using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImageUploadLogUnitTests
    {
        [Theory]
        [InlineData(2_000_000, 500_000, 1_500_000)]
        [InlineData(500_000, 500_000, 0)]
        [InlineData(400_000, 550_000, 0)]
        public void CalculateBytesSaved_ReturnsNonNegativeDifference(long originalBytes, long optimizedBytes, long expectedSaved)
        {
            Assert.Equal(expectedSaved, MedicationImageUploadLog.CalculateBytesSaved(originalBytes, optimizedBytes));
        }
    }
}
