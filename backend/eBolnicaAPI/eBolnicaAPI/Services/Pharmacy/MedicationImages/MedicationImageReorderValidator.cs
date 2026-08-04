using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public static class MedicationImageReorderValidator
    {
        public static async Task ValidateAllImageIdsBelongToMedicationAsync(
            AppDbContext context,
            int medicationId,
            IReadOnlyList<int> imageIds,
            IReadOnlyCollection<MedicationImage> medicationImages,
            CancellationToken cancellationToken = default)
        {
            var ownedIds = medicationImages
                .Where(image => image.MedicationId == medicationId)
                .Select(image => image.Id)
                .ToHashSet();

            var unownedIds = imageIds.Where(id => !ownedIds.Contains(id)).Distinct().ToList();
            if (unownedIds.Count == 0)
            {
                return;
            }

            var belongsToAnotherMedication = await context.MedicationImages
                .AnyAsync(
                    image => unownedIds.Contains(image.Id) && image.MedicationId != medicationId,
                    cancellationToken);

            throw new MedicationImageValidationException(
                belongsToAnotherMedication
                    ? "One or more image ids do not belong to this medication."
                    : "One or more image ids were not found for this medication.");
        }
    }
}
