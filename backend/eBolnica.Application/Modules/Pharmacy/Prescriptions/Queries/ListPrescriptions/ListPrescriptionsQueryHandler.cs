using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.ListPrescriptions;

public sealed class ListPrescriptionsQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListPrescriptionsQuery, ListPrescriptionsQueryDto>
{
    public async Task<ListPrescriptionsQueryDto> Handle(ListPrescriptionsQuery request, CancellationToken ct)
    {
        var query = PrescriptionQueryFilters.Apply(
            ctx.Prescriptions.AsNoTracking(),
            request.Status,
            request.Search,
            request.PrescribedFrom,
            request.PrescribedTo,
            request.PatientSearch,
            request.DoctorSearch);

        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new PrescriptionListSummaryDto
            {
                TotalPrescriptions = g.Count(),
                PendingPrescriptions = g.Count(p => p.Status == PrescriptionStatuses.Pending),
                DispensedPrescriptions = g.Count(p => p.Status == PrescriptionStatuses.Dispensed),
                TotalRevenue = g.Where(p => p.Status == PrescriptionStatuses.Dispensed).Sum(p => p.TotalAmount)
            })
            .FirstOrDefaultAsync(ct) ?? new PrescriptionListSummaryDto();

        var totalCount = await query.CountAsync(ct);
        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        PharmacySortValidator.ValidatePrescriptionSort(request.SortBy);
        query = PrescriptionQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .WithDetails()
            .ToListAsync(ct);

        return new ListPrescriptionsQueryDto
        {
            Items = items.Select(PrescriptionMapping.MapToDto).ToList(),
            Summary = summary,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
