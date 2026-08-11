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
    /// <summary>True only when at least one medication row was persisted.</summary>
    public bool Committed { get; set; }
    /// <summary>True when some rows were saved and others were skipped due to validation errors.</summary>
    public bool IsPartialImport { get; set; }
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
