namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.ListPrescriptions;

using eBolnica.Application.Modules.Pharmacy.Prescriptions;

public sealed class ListPrescriptionsQuery : IRequest<ListPrescriptionsQueryDto>
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string? PatientSearch { get; set; }
    public string? DoctorSearch { get; set; }
    public DateTime? PrescribedFrom { get; set; }
    public DateTime? PrescribedTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}

public sealed class PrescriptionListSummaryDto
{
    public int TotalPrescriptions { get; set; }
    public int PendingPrescriptions { get; set; }
    public int DispensedPrescriptions { get; set; }
    public decimal TotalRevenue { get; set; }
}

public sealed class ListPrescriptionsQueryDto
{
    public IReadOnlyList<PrescriptionDto> Items { get; set; } = Array.Empty<PrescriptionDto>();
    public PrescriptionListSummaryDto Summary { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
