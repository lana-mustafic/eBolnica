using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

public sealed class GetInventoryQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetInventoryQuery, GetInventoryQueryDto>
{
    private const int MaxAlertItems = 20;

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

        var expiryThreshold = DateTime.UtcNow.AddDays(30);
        var now = DateTime.UtcNow;

        var lowStockQuery = query.Where(m => m.StockQuantity < m.MinimumStockLevel);
        var expiryQuery = query.Where(m => m.ExpiryDate.HasValue
            && m.ExpiryDate.Value <= expiryThreshold
            && m.ExpiryDate.Value > now);

        var stats = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalCount = g.Count(),
                LowStockAlertCount = g.Count(m => m.StockQuantity < m.MinimumStockLevel),
                ExpiryAlertCount = g.Count(m =>
                    m.ExpiryDate.HasValue
                    && m.ExpiryDate.Value <= expiryThreshold
                    && m.ExpiryDate.Value > now)
            })
            .FirstOrDefaultAsync(ct);

        var totalCount = stats?.TotalCount ?? 0;
        var lowStockAlertCount = stats?.LowStockAlertCount ?? 0;
        var expiryAlertCount = stats?.ExpiryAlertCount ?? 0;

        var lowStockAlerts = await lowStockQuery
            .OrderBy(m => m.StockQuantity)
            .ThenBy(m => m.Name)
            .Take(MaxAlertItems)
            .Select(MedicationMapping.ToDtoExpression)
            .ToListAsync(ct);

        var expiryAlerts = await expiryQuery
            .OrderBy(m => m.ExpiryDate)
            .ThenBy(m => m.Name)
            .Take(MaxAlertItems)
            .Select(MedicationMapping.ToDtoExpression)
            .ToListAsync(ct);

        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

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
            LowStockAlertCount = lowStockAlertCount,
            ExpiryAlertCount = expiryAlertCount,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
