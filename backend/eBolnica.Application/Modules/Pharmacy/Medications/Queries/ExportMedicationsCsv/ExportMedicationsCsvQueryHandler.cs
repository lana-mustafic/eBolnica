using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Csv;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ExportMedicationsCsv;

public sealed class ExportMedicationsCsvQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ExportMedicationsCsvQuery, ExportMedicationsCsvQueryDto>
{
    public async Task<ExportMedicationsCsvQueryDto> Handle(ExportMedicationsCsvQuery request, CancellationToken ct)
    {
        var query = MedicationQueryFilters.Apply(
            ctx.Medications.AsNoTracking(),
            request.Search,
            request.Category,
            request.IsActive,
            request.IncludeInactive,
            request.StockStatus);

        query = MedicationQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var total = await query.CountAsync(ct);
        if (total > MedicationCsvService.MaxExportRows)
            throw new eBolnicaBusinessRuleException(
                "export.limit_exceeded",
                $"Export is limited to {MedicationCsvService.MaxExportRows} rows. Refine filters.");

        var medications = await query.Take(MedicationCsvService.MaxExportRows).ToListAsync(ct);

        return new ExportMedicationsCsvQueryDto
        {
            FileName = MedicationCsvService.GetExportFileName(),
            CsvContent = MedicationCsvService.BuildExportCsv(medications)
        };
    }
}
