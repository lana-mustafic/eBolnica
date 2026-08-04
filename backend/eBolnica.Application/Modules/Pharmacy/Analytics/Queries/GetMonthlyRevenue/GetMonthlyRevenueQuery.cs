using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Application.Modules.Pharmacy.Analytics.Queries.GetMonthlyRevenue;

namespace eBolnica.Application.Modules.Pharmacy.Analytics.Queries.GetMonthlyRevenue;

public sealed class GetMonthlyRevenueQuery : IRequest<RevenueDataDto>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Months { get; set; } = 12;
}

public sealed class GetMonthlyRevenueQueryHandler(IPharmacyAnalyticsService analytics)
    : IRequestHandler<GetMonthlyRevenueQuery, RevenueDataDto>
{
    public Task<RevenueDataDto> Handle(GetMonthlyRevenueQuery request, CancellationToken ct) =>
        analytics.GetMonthlyRevenueAsync(request.StartDate, request.EndDate, request.Months, ct);
}
