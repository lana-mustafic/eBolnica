using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using eBolnicaAPI.Services.Pharmacy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationCsvImportServiceUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MedicationCsvImportService _service;

        public MedicationCsvImportServiceUnitTests()
        {
            _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            _context.Medications.Add(new Medication
            {
                Name = "Existing Med",
                Category = "painkiller",
                Price = 5m,
                StockQuantity = 10,
                MinimumStockLevel = 5,
                IsActive = true,
                RequiresPrescription = false,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var analytics = new Mock<IPharmacyAnalyticsService>();
            _service = new MedicationCsvImportService(_context, analytics.Object);
        }

        [Fact]
        public async Task ImportAsync_ValidRow_InsertsMedication()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Import Med,,Vitamins,,,12.50,25,5,{expiry},,,,No,Yes
                """;

            var (fileError, summary) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.Null(fileError);
            Assert.NotNull(summary);
            Assert.Equal(1, summary!.SuccessCount);
            Assert.Equal(0, summary.FailureCount);
            Assert.Contains(_context.Medications, m => m.Name == "Import Med");
        }

        [Fact]
        public async Task ImportAsync_DuplicateName_ReturnsRowError()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Existing Med,,Vitamins,,,12.50,25,5,{expiry},,,,No,Yes
                """;

            var (_, summary) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(summary);
            Assert.Equal(0, summary!.SuccessCount);
            Assert.Equal(1, summary.FailureCount);
            Assert.Contains(summary.Errors, e => e.Field == "Name" && e.Reason.Contains("already exists"));
        }

        [Fact]
        public async Task ImportAsync_InvalidHeaders_ReturnsFileError()
        {
            var csv = "Wrong,Headers\nfoo,bar";

            var (fileError, summary) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(fileError);
            Assert.Null(summary);
            Assert.Contains("Missing required column", fileError);
        }

        [Fact]
        public async Task ImportAsync_MalformedCsv_ReturnsFileError()
        {
            var (fileError, summary) = await _service.ImportAsync(CreateFormFile("Name\n\"unclosed"));

            Assert.NotNull(fileError);
            Assert.Null(summary);
            Assert.Contains("Malformed CSV", fileError);
        }

        [Fact]
        public async Task ImportAsync_PartialImport_ImportsValidRowsOnly()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Good Med,,Vitamins,,,12.50,25,5,{expiry},,,,No,Yes
                Bad,,Vitamins,,,not-a-price,25,5,{expiry},,,,No,Yes
                """;

            var (_, summary) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(summary);
            Assert.Equal(1, summary!.SuccessCount);
            Assert.Equal(1, summary.FailureCount);
            Assert.Contains(_context.Medications, m => m.Name == "Good Med");
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private static FormFile CreateFormFile(string csv, string fileName = "medications.csv")
        {
            var bytes = Encoding.UTF8.GetBytes(csv);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };
        }
    }
}
