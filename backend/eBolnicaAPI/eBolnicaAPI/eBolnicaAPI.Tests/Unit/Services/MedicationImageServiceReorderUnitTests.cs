using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImageServiceReorderUnitTests : IDisposable
    {
        private readonly AppDbContext _context;

        public MedicationImageServiceReorderUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _context.Medications.Add(new Medication
            {
                Id = 7,
                Name = "Reorder Med",
                NormalizedName = "reorder med",
                Category = "Test",
                Price = 10,
                StockQuantity = 1,
                MinimumStockLevel = 1,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                RequiresPrescription = false
            });

            _context.Medications.Add(new Medication
            {
                Id = 8,
                Name = "Other Med",
                NormalizedName = "other med",
                Category = "Test",
                Price = 10,
                StockQuantity = 1,
                MinimumStockLevel = 1,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                RequiresPrescription = false
            });

            _context.MedicationImages.AddRange(
                CreateImage(11, 0, true),
                CreateImage(12, 1, false),
                CreateImage(13, 2, false),
                CreateImage(99, 0, true, medicationId: 8));
            _context.SaveChanges();
        }

        [Fact]
        public async Task ReorderImagesAsync_UpdatesSortOrderSequentiallyFromZero()
        {
            var service = CreateService();

            var result = await service.ReorderImagesAsync(7, new[] { 13, 11, 12 });

            Assert.Equal(new[] { 13, 11, 12 }, result.Select(image => image.Id).ToArray());
            Assert.Equal(new[] { 0, 1, 2 }, result.Select(image => image.SortOrder).ToArray());

            var persisted = await _context.MedicationImages
                .Where(image => image.MedicationId == 7)
                .OrderBy(image => image.SortOrder)
                .Select(image => new { image.Id, image.SortOrder })
                .ToListAsync();

            Assert.Equal(new[] { 13, 11, 12 }, persisted.Select(image => image.Id).ToArray());
            Assert.Equal(new[] { 0, 1, 2 }, persisted.Select(image => image.SortOrder).ToArray());
        }

        [Fact]
        public async Task ReorderImagesAsync_KeepsPrimaryFlagOnSameImage()
        {
            var service = CreateService();

            var result = await service.ReorderImagesAsync(7, new[] { 12, 13, 11 });

            Assert.True(result.Single(image => image.Id == 11).IsPrimary);
            Assert.Equal(1, result.Count(image => image.IsPrimary));
        }

        [Fact]
        public async Task ReorderImagesAsync_ThrowsWhenImageIdsAreIncomplete()
        {
            var service = CreateService();

            var exception = await Assert.ThrowsAsync<MedicationImageValidationException>(
                () => service.ReorderImagesAsync(7, new[] { 11, 12 }));

            Assert.Contains("every medication image exactly once", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReorderImagesAsync_ThrowsWhenImageIdsContainForeignImage()
        {
            var service = CreateService();

            var exception = await Assert.ThrowsAsync<MedicationImageValidationException>(
                () => service.ReorderImagesAsync(7, new[] { 11, 12, 99 }));

            Assert.Contains("do not belong to this medication", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReorderImagesAsync_ThrowsWhenImageIdDoesNotExist()
        {
            var service = CreateService();

            var exception = await Assert.ThrowsAsync<MedicationImageValidationException>(
                () => service.ReorderImagesAsync(7, new[] { 11, 12, 404 }));

            Assert.Contains("not found for this medication", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteImageAsync_CompactsRemainingSortOrdersSequentiallyFromZero()
        {
            var service = CreateService();

            await service.DeleteImageAsync(7, 12);

            var remaining = await _context.MedicationImages
                .Where(image => image.MedicationId == 7)
                .OrderBy(image => image.SortOrder)
                .Select(image => new { image.Id, image.SortOrder })
                .ToListAsync();

            Assert.Equal(new[] { 11, 13 }, remaining.Select(image => image.Id).ToArray());
            Assert.Equal(new[] { 0, 1 }, remaining.Select(image => image.SortOrder).ToArray());
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private MedicationImageService CreateService()
        {
            return new MedicationImageService(
                _context,
                Mock.Of<IMedicationImageFileValidator>(),
                Mock.Of<IMedicationImageVirusScanner>(),
                Mock.Of<IMedicationImageOptimizer>(),
                Mock.Of<IMedicationImageStorageService>(),
                Mock.Of<ILogger<MedicationImageService>>());
        }

        private static MedicationImage CreateImage(int id, int sortOrder, bool isPrimary, int medicationId = 7)
        {
            return new MedicationImage
            {
                Id = id,
                MedicationId = medicationId,
                FileName = $"image-{id}.jpg",
                RelativeUrl = $"/uploads/medications/{medicationId}/original/{id}.jpg",
                ThumbnailRelativeUrl = $"/uploads/medications/{medicationId}/thumbnails/{id}.jpg",
                IsPrimary = isPrimary,
                SortOrder = sortOrder,
                UploadedAt = DateTime.UtcNow,
                FileSizeBytes = 1024,
                Width = 128,
                Height = 128
            };
        }
    }
}
