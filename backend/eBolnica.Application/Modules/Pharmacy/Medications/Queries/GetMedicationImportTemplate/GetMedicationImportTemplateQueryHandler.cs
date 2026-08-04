using eBolnica.Application.Modules.Pharmacy.Medications.Csv;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationImportTemplate;

public sealed class GetMedicationImportTemplateQueryHandler
    : IRequestHandler<GetMedicationImportTemplateQuery, GetMedicationImportTemplateQueryDto>
{
    public Task<GetMedicationImportTemplateQueryDto> Handle(
        GetMedicationImportTemplateQuery request, CancellationToken ct)
        => Task.FromResult(new GetMedicationImportTemplateQueryDto
        {
            FileName = MedicationCsvService.GetImportTemplateFileName(),
            CsvContent = MedicationCsvService.BuildImportTemplateCsv()
        });
}
