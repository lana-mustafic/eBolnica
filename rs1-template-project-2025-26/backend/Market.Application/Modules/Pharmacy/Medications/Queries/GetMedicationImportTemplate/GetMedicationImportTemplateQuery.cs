namespace Market.Application.Modules.Pharmacy.Medications.Queries.GetMedicationImportTemplate;

public sealed class GetMedicationImportTemplateQuery : IRequest<GetMedicationImportTemplateQueryDto>
{
}

public sealed class GetMedicationImportTemplateQueryDto
{
    public string FileName { get; init; } = string.Empty;
    public string CsvContent { get; init; } = string.Empty;
}
