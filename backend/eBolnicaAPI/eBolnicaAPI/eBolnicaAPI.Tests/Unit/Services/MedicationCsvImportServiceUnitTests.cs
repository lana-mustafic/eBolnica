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
            var duplicateChecker = new MedicationImportDuplicateChecker(_context);
            _service = new MedicationCsvImportService(_context, analytics.Object, duplicateChecker);
        }

        [Fact]
        public async Task ImportAsync_ValidRow_InsertsMedication()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Import Med,,Vitamins,,,12.50,25,5,{expiry},,,,No,Yes
                """;

            var (fileError, result) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.Null(fileError);
            Assert.NotNull(result);
            Assert.Equal(1, result!.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.True(result.Committed);
            Assert.Single(result.ImportedMedicationIds);
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

            var (_, result) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(result);
            Assert.Equal(0, result!.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            Assert.True(result.Committed);
            Assert.Empty(result.ImportedMedicationIds);
            Assert.Contains(result.Errors, e =>
                e.Field == "Name" && e.Reason.Contains("already exists"));
        }

        [Fact]
        public async Task ImportAsync_DuplicateNameCaseInsensitive_ReturnsRowError()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                existing med,,Vitamins,,,12.50,25,5,{expiry},,,,No,Yes
                """;

            var (_, result) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(result);
            Assert.Equal(0, result!.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            Assert.Contains(result.Errors, e => e.Field == "Name");
        }

        [Fact]
        public async Task ImportAsync_DuplicateNameWithinFile_RejectsSecondRow()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Unique Batch Med,,Vitamins,,,12.50,25,5,{expiry},,,,No,Yes
                unique batch med,,Vitamins,,,13.50,30,6,{expiry},,,,No,Yes
                """;

            var (_, result) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(result);
            Assert.Equal(1, result!.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            Assert.Contains(result.Errors, e =>
                e.Field == "Name" && e.Reason.Contains("Duplicate name in this import file"));
        }

        [Fact]
        public async Task ImportAsync_InvalidHeaders_ReturnsFileError()
        {
            var csv = "Wrong,Headers\nfoo,bar";

            var (fileError, result) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(fileError);
            Assert.Null(result);
            Assert.Contains("Missing required column", fileError);
        }

        [Fact]
        public async Task ImportAsync_MalformedCsv_ReturnsFileError()
        {
            var (fileError, result) = await _service.ImportAsync(CreateFormFile("Name\n\"unclosed"));

            Assert.NotNull(fileError);
            Assert.Null(result);
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

            var (_, result) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(result);
            Assert.Equal(1, result!.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            Assert.True(result.Committed);
            Assert.Single(result.ImportedMedicationIds);
            Assert.Contains(_context.Medications, m => m.Name == "Good Med");
        }

        [Fact]
        public async Task ImportAsync_MultipleValidRows_CommitsSingleBatch()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Batch Med A,,Vitamins,,,12.50,25,5,{expiry},,,,No,Yes
                Batch Med B,,Vitamins,,,13.50,30,6,{expiry},,,,No,Yes
                """;

            var (_, result) = await _service.ImportAsync(CreateFormFile(csv));

            Assert.NotNull(result);
            Assert.Equal(2, result!.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.True(result.Committed);
            Assert.Equal(2, result.ImportedMedicationIds.Count);
            Assert.Contains(_context.Medications, m => m.Name == "Batch Med A");
            Assert.Contains(_context.Medications, m => m.Name == "Batch Med B");
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
