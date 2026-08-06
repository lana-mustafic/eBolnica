using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Application.Modules.Pharmacy.Analytics.Queries.GetStockTrends;

namespace eBolnica.Application.Modules.Pharmacy.Analytics.Queries.GetStockTrends;

public sealed class GetStockTrendsQuery : IRequest<StockTrendsDataDto>
{
    public int[]? MedicationIds { get; set; }
}

public sealed class GetStockTrendsQueryHandler(IPharmacyAnalyticsService analytics)
    : IRequestHandler<GetStockTrendsQuery, StockTrendsDataDto>
{
    public Task<StockTrendsDataDto> Handle(GetStockTrendsQuery request, CancellationToken ct) =>
        analytics.GetStockTrendsAsync(request.MedicationIds, ct);
}
