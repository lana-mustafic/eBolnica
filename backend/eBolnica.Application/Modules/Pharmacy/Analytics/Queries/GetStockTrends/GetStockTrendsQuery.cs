using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Application.Modules.Pharmacy.Analytics.Queries.GetStockTrends;

namespace eBolnica.Application.Modules.Pharmacy.Analytics.Queries.GetStockTrends;

public sealed class GetStockTrendsQuery : IRequest<StockTrendsDataDto>
{
    public int[]? MedicationIds { get; set; }
    public int Days { get; set; } = 30;
    public string Interval { get; set; } = "daily";
}

public sealed class GetStockTrendsQueryHandler(IPharmacyAnalyticsService analytics)
    : IRequestHandler<GetStockTrendsQuery, StockTrendsDataDto>
{
    public Task<StockTrendsDataDto> Handle(GetStockTrendsQuery request, CancellationToken ct) =>
        analytics.GetStockTrendsAsync(request.MedicationIds, request.Days, request.Interval, ct);
}
