using eBolnica.Application.Modules.Pharmacy.Analytics;

namespace eBolnica.Application.Abstractions;

public interface IPharmacyAnalyticsService
{
    Task<DashboardStatsResponseDto> GetDashboardStatsAsync(
        DateTime? startDate,
        DateTime? endDate,
        int revenueMonths = 12,
        int topCategoriesCount = 8,
        int[]? medicationIds = null,
        CancellationToken ct = default);

    Task<RevenueDataDto> GetMonthlyRevenueAsync(
        DateTime? startDate,
        DateTime? endDate,
        int months = 12,
        CancellationToken ct = default);

    Task<CategoriesDataDto> GetTopCategoriesAsync(int topCount = 8, CancellationToken ct = default);

    Task<StockTrendsDataDto> GetStockTrendsAsync(
        int[]? medicationIds = null,
        CancellationToken ct = default);

    void InvalidateAnalyticsCache();
}
