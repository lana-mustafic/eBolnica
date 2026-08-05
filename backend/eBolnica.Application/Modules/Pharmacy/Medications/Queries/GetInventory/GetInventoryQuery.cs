using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;

public sealed class GetInventoryQuery : IRequest<GetInventoryQueryDto>
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public string? StockStatus { get; init; }
    public bool? RequiresPrescription { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}

public sealed class GetInventoryQueryDto
{
    public IReadOnlyList<MedicationDto> Items { get; init; } = Array.Empty<MedicationDto>();
    public IReadOnlyList<MedicationDto> LowStockAlerts { get; init; } = Array.Empty<MedicationDto>();
    public IReadOnlyList<MedicationDto> ExpiryAlerts { get; init; } = Array.Empty<MedicationDto>();
    public int TotalCount { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
