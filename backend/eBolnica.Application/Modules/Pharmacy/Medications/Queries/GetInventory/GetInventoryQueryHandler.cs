using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;

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

        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        PharmacySortValidator.ValidateMedicationSort(request.SortBy);
        var sortedQuery = MedicationQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var stats = await query
            .GroupBy(_ => 1)
            .Select(g => new FilteredInventoryStats
            {
                TotalCount = g.Count(),
                LowStockAlertCount = g.Count(m => m.StockQuantity < m.MinimumStockLevel),
                ExpiryAlertCount = g.Count(m =>
                    m.ExpiryDate.HasValue
                    && m.ExpiryDate.Value <= expiryThreshold
                    && m.ExpiryDate.Value > now)
            })
            .FirstOrDefaultAsync(ct);

        var globalStats = await ctx.Medications
            .AsNoTracking()
            .Where(m => m.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new GlobalInventoryStats
            {
                TotalMedications = g.Count(),
                InventoryValue = g.Sum(m => m.Price * m.StockQuantity)
            })
            .FirstOrDefaultAsync(ct);

        var lowStockAlerts = await lowStockQuery
            .OrderBy(m => m.StockQuantity)
            .ThenBy(m => m.Name)
            .Take(MaxAlertItems)
            .Select(MedicationMapping.ToListDtoExpression)
            .ToListAsync(ct);

        var expiryAlerts = await expiryQuery
            .OrderBy(m => m.ExpiryDate)
            .ThenBy(m => m.Name)
            .Take(MaxAlertItems)
            .Select(MedicationMapping.ToListDtoExpression)
            .ToListAsync(ct);

        var items = await sortedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MedicationMapping.ToListDtoExpression)
            .ToListAsync(ct);

        var totalCount = stats?.TotalCount ?? 0;

        return new GetInventoryQueryDto
        {
            Items = items,
            LowStockAlerts = lowStockAlerts,
            ExpiryAlerts = expiryAlerts,
            LowStockAlertCount = stats?.LowStockAlertCount ?? 0,
            ExpiryAlertCount = stats?.ExpiryAlertCount ?? 0,
            TotalMedications = globalStats?.TotalMedications ?? 0,
            InventoryValue = globalStats?.InventoryValue ?? 0,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private sealed class FilteredInventoryStats
    {
        public int TotalCount { get; init; }
        public int LowStockAlertCount { get; init; }
        public int ExpiryAlertCount { get; init; }
    }

    private sealed class GlobalInventoryStats
    {
        public int TotalMedications { get; init; }
        public decimal InventoryValue { get; init; }
    }
}
