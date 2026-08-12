using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Images;

internal static class MedicationPrimaryImageSync
{
    public static void Apply(MedicationEntity medication)
    {
        var primary = medication.Images
            .Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();

        medication.PrimaryImageId = primary?.Id;
        medication.ImageUrl = primary?.RelativeUrl;
    }
}
