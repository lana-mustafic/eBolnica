namespace eBolnica.Application.Modules.Doctor.Prescriptions.Queries.ListDoctorPrescriptions;

using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.ListPrescriptions;
using eBolnica.Domain.Entities.Pharmacy;

public sealed class ListDoctorPrescriptionsQuery : IRequest<ListPrescriptionsQueryDto>
{
    public string? Status { get; init; }
    public int? PatientId { get; init; }
    public string? Search { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}

public sealed class ListDoctorPrescriptionsQueryValidator : AbstractValidator<ListDoctorPrescriptionsQuery>
{
    public ListDoctorPrescriptionsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class ListDoctorPrescriptionsQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<ListDoctorPrescriptionsQuery, ListPrescriptionsQueryDto>
{
    public async Task<ListPrescriptionsQueryDto> Handle(ListDoctorPrescriptionsQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new eBolnicaNotFoundException("Doctor profile not found.");

        var query = ctx.Prescriptions.AsNoTracking().Where(p => p.DoctorId == doctor.Id);

        if (request.PatientId.HasValue)
            query = query.Where(p => p.PatientId == request.PatientId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status) && !string.Equals(request.Status, "All", StringComparison.OrdinalIgnoreCase))
            query = query.Where(p => p.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.PrescriptionNumber.Contains(term)
                || p.Patient.FirstName.Contains(term)
                || p.Patient.LastName.Contains(term));
        }

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
