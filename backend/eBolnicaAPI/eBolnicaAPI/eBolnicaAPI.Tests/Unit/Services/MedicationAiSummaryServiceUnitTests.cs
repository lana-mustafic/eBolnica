using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.Services;
using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAiSummaryServiceUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IMedicationAiSummaryClient> _clientMock;
        private readonly Mock<ILogger<MedicationAiSummaryService>> _loggerMock;

        public MedicationAiSummaryServiceUnitTests()
        {
            _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            _context.Medications.Add(new Medication
            {
                Id = 10,
                Name = "Aspirin",
                NormalizedName = "aspirin",
                Category = "painkiller",
                Description = "Pain relief medication",
                DosageForm = "tablet",
                Strength = "500mg",
                Price = 8.5m,
                StockQuantity = 100,
                MinimumStockLevel = 20,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                RequiresPrescription = false,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            _clientMock = new Mock<IMedicationAiSummaryClient>();
            _loggerMock = new Mock<ILogger<MedicationAiSummaryService>>();
        }

        [Fact]
        public async Task GenerateSummaryAsync_ReturnsParsedSummary()
        {
            _clientMock
                .Setup(client => client.GenerateSummaryJsonAsync(
                    It.IsAny<string>(),
                    It.Is<string>(prompt => prompt.Contains("name: Aspirin")),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("""
                    {
                      "overview": "Aspirin overview",
                      "usageNotes": "Use as directed",
                      "stockExpiryAlert": "Stock is healthy",
                      "prescriptionRequirement": "No prescription required"
                    }
                    """);

            var service = CreateService(new MedicationAiSummarySettings
            {
                Enabled = true,
                ApiKey = "test-key"
            });

            var summary = await service.GenerateSummaryAsync(10);

            Assert.Equal("Aspirin overview", summary.Overview);
            Assert.Equal("Use as directed", summary.UsageNotes);
            Assert.Equal("Stock is healthy", summary.StockExpiryAlert);
            Assert.Equal("No prescription required", summary.PrescriptionRequirement);
        }

        [Fact]
        public async Task GenerateSummaryAsync_UnknownMedication_ThrowsKeyNotFound()
        {
            var service = CreateService(new MedicationAiSummarySettings
            {
                Enabled = true,
                ApiKey = "test-key"
            });

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GenerateSummaryAsync(999));
        }

        [Fact]
        public async Task GenerateSummaryAsync_MissingApiKey_ThrowsUnavailable()
        {
            var service = CreateService(new MedicationAiSummarySettings
            {
                Enabled = true,
                ApiKey = string.Empty
            });

            await Assert.ThrowsAsync<MedicationAiSummaryUnavailableException>(
                () => service.GenerateSummaryAsync(10));
        }

        [Fact]
        public async Task GenerateSummaryAsync_ClientFailure_ThrowsUnavailable()
        {
            _clientMock
                .Setup(client => client.GenerateSummaryJsonAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(MedicationAiSummaryUnavailableException.ServiceUnavailable("provider down"));

            var service = CreateService(new MedicationAiSummarySettings
            {
                Enabled = true,
                ApiKey = "test-key"
            });

            await Assert.ThrowsAsync<MedicationAiSummaryUnavailableException>(
                () => service.GenerateSummaryAsync(10));
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private MedicationAiSummaryService CreateService(MedicationAiSummarySettings settings) =>
            new(
                _context,
                _clientMock.Object,
                Options.Create(settings),
                _loggerMock.Object);
    }
}
