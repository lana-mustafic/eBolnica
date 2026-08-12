using eBolnica.Domain.Entities.Pharmacy;
using System.Linq.Expressions;

namespace eBolnica.Application.Modules.Pharmacy.Medications;

internal static class MedicationMapping
{
    /// <summary>
    /// List/inventory projection with primary image id for authenticated file access.
    /// </summary>
    public static readonly Expression<Func<MedicationEntity, MedicationDto>> ToListDtoExpression = m =>
        new MedicationDto
        {
            Id = m.Id,
            Name = m.Name,
            GenericName = m.GenericName,
            Description = m.Description,
            Manufacturer = m.Manufacturer,
            Price = m.Price,
            StockQuantity = m.StockQuantity,
            MinimumStockLevel = m.MinimumStockLevel,
            ExpiryDate = m.ExpiryDate,
            BatchNumber = m.BatchNumber,
            IsActive = m.IsActive,
            RequiresPrescription = m.RequiresPrescription,
            Category = m.Category,
            DosageForm = m.DosageForm,
            Strength = m.Strength,
            CreatedAt = m.CreatedAtUtc,
            UpdatedAt = m.ModifiedAtUtc,
            RowVersion = m.RowVersion,
            PrimaryImageUrl = m.ImageUrl,
            PrimaryImageId = m.PrimaryImageId
        };

    /// <summary>
    /// Detail projection with image-collection fallback when ImageUrl is not yet denormalized.
    /// </summary>
    public static readonly Expression<Func<MedicationEntity, MedicationDto>> ToDetailDtoExpression = m =>
        new MedicationDto
        {
            Id = m.Id,
            Name = m.Name,
            GenericName = m.GenericName,
            Description = m.Description,
            Manufacturer = m.Manufacturer,
            Price = m.Price,
            StockQuantity = m.StockQuantity,
            MinimumStockLevel = m.MinimumStockLevel,
            ExpiryDate = m.ExpiryDate,
            BatchNumber = m.BatchNumber,
            IsActive = m.IsActive,
            RequiresPrescription = m.RequiresPrescription,
            Category = m.Category,
            DosageForm = m.DosageForm,
            Strength = m.Strength,
            CreatedAt = m.CreatedAtUtc,
            UpdatedAt = m.ModifiedAtUtc,
            RowVersion = m.RowVersion,
            PrimaryImageUrl = m.ImageUrl ?? m.Images
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.RelativeUrl)
                .FirstOrDefault(),
            PrimaryImageId = m.PrimaryImageId ?? m.Images
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => (int?)i.Id)
                .FirstOrDefault()
        };
}
