namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

public sealed class ListMedicationsQuery : IRequest<ListMedicationsQueryDto>
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public bool? IsActive { get; init; }
    public bool IncludeInactive { get; init; }
    public string? StockStatus { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}

public sealed class ListMedicationsQueryDto
{
    public IReadOnlyList<MedicationDto> Items { get; init; } = Array.Empty<MedicationDto>();
    public int TotalCount { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
