using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

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
            request.StockStatus,
            request.RequiresPrescription);

        var totalCount = await query.CountAsync(ct);
        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        PharmacySortValidator.ValidateMedicationSort(request.SortBy);
        query = MedicationQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MedicationMapping.ToListDtoExpression)
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
