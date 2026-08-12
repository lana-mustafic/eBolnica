namespace eBolnica.Application.Modules.Pharmacy.Activities.Queries.ListRecentActivities;

using eBolnica.Domain.Entities.Pharmacy;

public sealed class ListRecentActivitiesQuery : IRequest<IReadOnlyList<PharmacyActivityDto>>
{
    public int Limit { get; init; } = 10;
    public string? Category { get; init; }
}

public sealed class ListRecentActivitiesQueryValidator : AbstractValidator<ListRecentActivitiesQuery>
{
    public ListRecentActivitiesQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
        RuleFor(x => x.Category)
            .Must(c => c is null || c is PharmacyActivityCategories.Prescription or PharmacyActivityCategories.Medication or PharmacyActivityCategories.Inventory)
            .WithMessage("Invalid activity category.");
    }
}

public sealed class ListRecentActivitiesQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListRecentActivitiesQuery, IReadOnlyList<PharmacyActivityDto>>
{
    public async Task<IReadOnlyList<PharmacyActivityDto>> Handle(ListRecentActivitiesQuery request, CancellationToken ct)
    {
        var query = ctx.PharmacyActivities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(a => a.Category == request.Category);

        return await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(request.Limit)
            .Select(a => new PharmacyActivityDto
            {
                Id = a.Id,
                EventType = a.EventType,
                Category = a.Category,
                Severity = a.Severity,
                Message = a.Message,
                OccurredAt = a.CreatedAtUtc,
                ActorName = a.ActorUserId == null
                    ? null
                    : ctx.Users
                        .Where(u => u.Id == a.ActorUserId && !u.IsDeleted)
                        .Select(u => u.Firstname + " " + u.Lastname)
                        .FirstOrDefault()
            })
            .ToListAsync(ct);
    }
}
