using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImageSortOrderUpdaterUnitTests
    {
        [Fact]
        public void ApplyOrderedImageIds_AssignsSequentialSortOrderFromZero()
        {
            var images = new Dictionary<int, MedicationImage>
            {
                [1] = CreateImage(1, 0),
                [2] = CreateImage(2, 1),
                [3] = CreateImage(3, 2)
            };

            MedicationImageSortOrderUpdater.ApplyOrderedImageIds(images, new[] { 3, 1, 2 });

            Assert.Equal(0, images[3].SortOrder);
            Assert.Equal(1, images[1].SortOrder);
            Assert.Equal(2, images[2].SortOrder);
        }

        [Fact]
        public void ApplyOrderedImageIds_ThrowsForUnknownImageId()
        {
            var images = new Dictionary<int, MedicationImage>
            {
                [1] = CreateImage(1, 0),
                [2] = CreateImage(2, 1)
            };

            var exception = Assert.Throws<MedicationImageValidationException>(
                () => MedicationImageSortOrderUpdater.ApplyOrderedImageIds(images, new[] { 1, 99 }));

            Assert.Contains("invalid image ids", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CompactSequentialSortOrders_RenormalizesAfterGap()
        {
            var images = new[]
            {
                CreateImage(1, 0),
                CreateImage(3, 2)
            };

            MedicationImageSortOrderUpdater.CompactSequentialSortOrders(images);

            Assert.Equal(new[] { 0, 1 }, images.OrderBy(image => image.Id).Select(image => image.SortOrder).ToArray());
        }

        private static MedicationImage CreateImage(int id, int sortOrder)
        {
            return new MedicationImage
            {
                Id = id,
                MedicationId = 7,
                FileName = $"image-{id}.jpg",
                RelativeUrl = $"/uploads/medications/7/original/{id}.jpg",
                SortOrder = sortOrder
            };
        }
    }
}
