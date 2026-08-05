using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

public sealed class GetInventoryQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetInventoryQuery, GetInventoryQueryDto>
{
    public async Task<GetInventoryQueryDto> Handle(GetInventoryQuery request, CancellationToken ct)
    {
        var query = MedicationQueryFilters.Apply(
            ctx.Medications.AsNoTracking(),
            request.Search,
            request.Category,
            isActive: true,
            includeInactive: false,
            request.StockStatus,
            request.RequiresPrescription);

        var totalCount = await query.CountAsync(ct);
        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var expiryThreshold = DateTime.UtcNow.AddDays(30);
        var now = DateTime.UtcNow;

        var lowStockAlerts = await query
            .Where(m => m.StockQuantity < m.MinimumStockLevel)
            .Select(MedicationMapping.ToDtoExpression)
            .ToListAsync(ct);

        var expiryAlerts = await query
            .Where(m => m.ExpiryDate.HasValue
                && m.ExpiryDate.Value <= expiryThreshold
                && m.ExpiryDate.Value > now)
            .Select(MedicationMapping.ToDtoExpression)
            .ToListAsync(ct);

        PharmacySortValidator.ValidateMedicationSort(request.SortBy);
        query = MedicationQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MedicationMapping.ToDtoExpression)
            .ToListAsync(ct);

        return new GetInventoryQueryDto
        {
            Items = items,
            LowStockAlerts = lowStockAlerts,
            ExpiryAlerts = expiryAlerts,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
