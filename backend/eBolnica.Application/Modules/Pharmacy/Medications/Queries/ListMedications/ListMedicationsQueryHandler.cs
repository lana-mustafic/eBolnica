using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;
using eBolnica.Domain.Entities.Pharmacy;
using System.Linq.Expressions;

public sealed class ListMedicationsQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListMedicationsQuery, ListMedicationsQueryDto>
{
    public async Task<ListMedicationsQueryDto> Handle(ListMedicationsQuery request, CancellationToken ct)
    {
        var query = MedicationQueryFilters.Apply(
            ctx.Medications.AsNoTracking(),
            request.Search,
            request.Category,
            request.IsActive,
            request.IncludeInactive,
            request.StockStatus);

        var totalCount = await query.CountAsync(ct);
        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        query = MedicationQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MedicationMapping.ToDtoExpression)
            .ToListAsync(ct);

        return new ListMedicationsQueryDto
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}

internal static class MedicationMapping
{
    public static readonly Expression<Func<MedicationEntity, MedicationDto>> ToDtoExpression = m =>
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
            PrimaryImageUrl = m.ImageUrl ?? m.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.RelativeUrl)
                .FirstOrDefault()
        };
}
