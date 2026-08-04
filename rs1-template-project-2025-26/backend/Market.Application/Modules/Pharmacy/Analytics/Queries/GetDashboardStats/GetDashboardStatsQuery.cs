using Market.Application.Modules.Pharmacy.Analytics;
using Market.Application.Modules.Pharmacy.Analytics.Queries.GetDashboardStats;

namespace Market.Application.Modules.Pharmacy.Analytics.Queries.GetDashboardStats;

public sealed class GetDashboardStatsQuery : IRequest<DashboardStatsResponseDto>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int RevenueMonths { get; set; } = 12;
    public int TopCategoriesCount { get; set; } = 8;
    public int[]? MedicationIds { get; set; }
    public int TrendDays { get; set; } = 30;
    public string TrendInterval { get; set; } = "daily";
}

public sealed class GetDashboardStatsQueryHandler(IPharmacyAnalyticsService analytics)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsResponseDto>
{
    public Task<DashboardStatsResponseDto> Handle(GetDashboardStatsQuery request, CancellationToken ct) =>
        analytics.GetDashboardStatsAsync(
            request.StartDate,
            request.EndDate,
            request.RevenueMonths,
            request.TopCategoriesCount,
            request.MedicationIds,
            request.TrendDays,
            request.TrendInterval,
            ct);
}
