using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Application.Modules.Pharmacy.Analytics.Queries.ExportInventoryPdf;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Csv;

namespace eBolnica.Application.Modules.Pharmacy.Analytics.Queries.ExportInventoryPdf;

public sealed class ExportInventoryPdfQuery : IRequest<PdfReportResultDto>
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeInactive { get; set; }
    public string? StockStatus { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}

public sealed class ExportInventoryPdfQueryHandler(IAppDbContext ctx, IPharmacyPdfReportService pdf)
    : IRequestHandler<ExportInventoryPdfQuery, PdfReportResultDto>
{
    public async Task<PdfReportResultDto> Handle(ExportInventoryPdfQuery request, CancellationToken ct)
    {
        var query = MedicationQueryFilters.Apply(
            ctx.Medications.AsNoTracking(),
            request.Search,
            request.Category,
            request.IsActive,
            request.IncludeInactive,
            request.StockStatus);

        PharmacySortValidator.ValidateMedicationSort(request.SortBy);
        query = MedicationQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var total = await query.CountAsync(ct);
        if (total > MedicationCsvService.MaxExportRows)
            throw new eBolnicaBusinessRuleException(
                "export.limit_exceeded",
                $"Export is limited to {MedicationCsvService.MaxExportRows} rows. Refine filters.");

        var medications = await query.Take(MedicationCsvService.MaxExportRows).ToListAsync(ct);
        var content = pdf.GenerateInventoryPdf(medications);

        return new PdfReportResultDto
        {
            Content = content,
            FileName = $"inventory-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf",
            RowCount = medications.Count
        };
    }
}
