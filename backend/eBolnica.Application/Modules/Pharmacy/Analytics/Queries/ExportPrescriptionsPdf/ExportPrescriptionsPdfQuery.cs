using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Application.Modules.Pharmacy.Analytics.Queries.ExportPrescriptionsPdf;
using eBolnica.Application.Modules.Pharmacy.Prescriptions;

namespace eBolnica.Application.Modules.Pharmacy.Analytics.Queries.ExportPrescriptionsPdf;

public sealed class ExportPrescriptionsPdfQuery : IRequest<PdfReportResultDto>
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}

public sealed class ExportPrescriptionsPdfQueryHandler(IAppDbContext ctx, IPharmacyPdfReportService pdf)
    : IRequestHandler<ExportPrescriptionsPdfQuery, PdfReportResultDto>
{
    private const int MaxExportRows = 10_000;

    public async Task<PdfReportResultDto> Handle(ExportPrescriptionsPdfQuery request, CancellationToken ct)
    {
        var query = PrescriptionQueryFilters.Apply(
            ctx.Prescriptions.AsNoTracking(),
            request.Status,
            request.Search);

        query = PrescriptionQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var total = await query.CountAsync(ct);
        if (total > MaxExportRows)
            throw new eBolnicaBusinessRuleException(
                "export.limit_exceeded",
                $"Export is limited to {MaxExportRows} rows. Refine filters.");

        var prescriptions = await query
            .Take(MaxExportRows)
            .WithDetails()
            .ToListAsync(ct);

        var content = pdf.GeneratePrescriptionsPdf(prescriptions);

        return new PdfReportResultDto
        {
            Content = content,
            FileName = $"prescriptions-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf",
            RowCount = prescriptions.Count
        };
    }
}
