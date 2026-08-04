namespace Market.Application.Modules.Pharmacy.Prescriptions.Queries.ListPrescriptions;

using Market.Application.Modules.Pharmacy.Prescriptions;

public sealed class ListPrescriptionsQuery : IRequest<ListPrescriptionsQueryDto>
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}

public sealed class ListPrescriptionsQueryDto
{
    public IReadOnlyList<PrescriptionDto> Items { get; set; } = Array.Empty<PrescriptionDto>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
