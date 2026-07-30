using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImageReorderValidatorUnitTests : IDisposable
    {
        private readonly AppDbContext _context;

        public MedicationImageReorderValidatorUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _context.Medications.AddRange(
                new Medication
                {
                    Id = 7,
                    Name = "Med A",
                    NormalizedName = "med a",
                    Category = "Test",
                    Price = 10,
                    StockQuantity = 1,
                    MinimumStockLevel = 1,
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    IsActive = true,
                    RequiresPrescription = false
                },
                new Medication
                {
                    Id = 8,
                    Name = "Med B",
                    NormalizedName = "med b",
                    Category = "Test",
                    Price = 10,
                    StockQuantity = 1,
                    MinimumStockLevel = 1,
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    IsActive = true,
                    RequiresPrescription = false
                });

            _context.MedicationImages.AddRange(
                CreateImage(11, 7),
                CreateImage(12, 7),
                CreateImage(99, 8));
            _context.SaveChanges();
        }

        [Fact]
        public async Task ValidateAllImageIdsBelongToMedicationAsync_AllowsOwnedIds()
        {
            var ownedImages = await _context.MedicationImages
                .Where(image => image.MedicationId == 7)
                .ToListAsync();

            var exception = await Record.ExceptionAsync(
                () => MedicationImageReorderValidator.ValidateAllImageIdsBelongToMedicationAsync(
                    _context,
                    7,
                    new[] { 11, 12 },
                    ownedImages));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ValidateAllImageIdsBelongToMedicationAsync_ThrowsWhenIdBelongsToAnotherMedication()
        {
            var ownedImages = await _context.MedicationImages
                .Where(image => image.MedicationId == 7)
                .ToListAsync();

            var exception = await Assert.ThrowsAsync<MedicationImageValidationException>(
                () => MedicationImageReorderValidator.ValidateAllImageIdsBelongToMedicationAsync(
                    _context,
                    7,
                    new[] { 11, 99 },
                    ownedImages));

            Assert.Contains("do not belong to this medication", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateAllImageIdsBelongToMedicationAsync_ThrowsWhenIdDoesNotExist()
        {
            var ownedImages = await _context.MedicationImages
                .Where(image => image.MedicationId == 7)
                .ToListAsync();

            var exception = await Assert.ThrowsAsync<MedicationImageValidationException>(
                () => MedicationImageReorderValidator.ValidateAllImageIdsBelongToMedicationAsync(
                    _context,
                    7,
                    new[] { 11, 404 },
                    ownedImages));

            Assert.Contains("not found for this medication", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private static MedicationImage CreateImage(int id, int medicationId)
        {
            return new MedicationImage
            {
                Id = id,
                MedicationId = medicationId,
                FileName = $"image-{id}.jpg",
                RelativeUrl = $"/uploads/medications/{medicationId}/original/{id}.jpg",
                SortOrder = 0
            };
        }
    }
}
