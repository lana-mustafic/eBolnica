using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.ExportMedicationsCsv;

public sealed class ExportMedicationsCsvQuery : IRequest<ExportMedicationsCsvQueryDto>
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public bool? IsActive { get; init; }
    public bool IncludeInactive { get; init; }
    public string? StockStatus { get; init; }
    public bool? RequiresPrescription { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}

public sealed class ExportMedicationsCsvQueryDto
{
    public string FileName { get; init; } = string.Empty;
    public string CsvContent { get; init; } = string.Empty;
}
