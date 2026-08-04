namespace eBolnica.Application.Modules.Pharmacy.Medications.Commands.ImportMedicationsCsv;

public sealed class ImportMedicationsCsvCommand : IRequest<MedicationImportResultDto>
{
    public string CsvContent { get; init; } = string.Empty;
}

public sealed class MedicationImportResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalRows { get; set; }
    public bool Committed { get; set; }
    public List<int> ImportedMedicationIds { get; set; } = [];
    public List<MedicationImportRowErrorDto> Errors { get; set; } = [];
    public string? BatchError { get; set; }
}

public sealed class MedicationImportRowErrorDto
{
    public int RowNumber { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Field { get; init; }
    public string? Value { get; init; }
}
