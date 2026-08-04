using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public static class MedicationImageSortOrderUpdater
    {
        public static void ApplyOrderedImageIds(
            IReadOnlyDictionary<int, MedicationImage> imageLookup,
            IReadOnlyList<int> imageIds)
        {
            for (var index = 0; index < imageIds.Count; index++)
            {
                if (!imageLookup.TryGetValue(imageIds[index], out var image))
                {
                    throw new MedicationImageValidationException(
                        "Image order contains invalid image ids for this medication.");
                }

                image.SortOrder = index;
            }
        }

        public static void CompactSequentialSortOrders(IEnumerable<MedicationImage> images)
        {
            var orderedImages = images.OrderBy(image => image.SortOrder).ToList();

            for (var index = 0; index < orderedImages.Count; index++)
            {
                orderedImages[index].SortOrder = index;
            }
        }
    }
}
