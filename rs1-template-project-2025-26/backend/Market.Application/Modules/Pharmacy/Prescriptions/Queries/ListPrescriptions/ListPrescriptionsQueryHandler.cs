using Market.Application.Modules.Pharmacy.Prescriptions;
using Market.Domain.Entities.Pharmacy;

namespace Market.Application.Modules.Pharmacy.Prescriptions.Queries.ListPrescriptions;

public sealed class ListPrescriptionsQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListPrescriptionsQuery, ListPrescriptionsQueryDto>
{
    public async Task<ListPrescriptionsQueryDto> Handle(ListPrescriptionsQuery request, CancellationToken ct)
    {
        var query = PrescriptionQueryFilters.Apply(
            ctx.Prescriptions.AsNoTracking(),
            request.Status,
            request.Search);

        var totalCount = await query.CountAsync(ct);
        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        query = PrescriptionQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .WithDetails()
            .ToListAsync(ct);

        return new ListPrescriptionsQueryDto
        {
            Items = items.Select(PrescriptionMapping.MapToDto).ToList(),
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
