using Market.Application.Modules.Pharmacy.Analytics;
using Market.Application.Modules.Pharmacy.Analytics.Queries.GetTopCategories;

namespace Market.Application.Modules.Pharmacy.Analytics.Queries.GetTopCategories;

public sealed class GetTopCategoriesQuery : IRequest<CategoriesDataDto>
{
    public int Limit { get; set; } = 8;
}

public sealed class GetTopCategoriesQueryHandler(IPharmacyAnalyticsService analytics)
    : IRequestHandler<GetTopCategoriesQuery, CategoriesDataDto>
{
    public Task<CategoriesDataDto> Handle(GetTopCategoriesQuery request, CancellationToken ct) =>
        analytics.GetTopCategoriesAsync(request.Limit, ct);
}
