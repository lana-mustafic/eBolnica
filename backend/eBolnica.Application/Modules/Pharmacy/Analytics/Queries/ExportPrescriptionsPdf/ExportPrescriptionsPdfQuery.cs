using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Application.Modules.Pharmacy.Analytics.Queries.ExportPrescriptionsPdf;
using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Analytics.Queries.ExportPrescriptionsPdf;

public sealed class ExportPrescriptionsPdfQuery : IRequest<PdfReportResultDto>
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string? PatientSearch { get; set; }
    public string? DoctorSearch { get; set; }
    public DateTime? PrescribedFrom { get; set; }
    public DateTime? PrescribedTo { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}

public sealed class ExportPrescriptionsPdfQueryHandler(IAppDbContext ctx, IPharmacyPdfReportService pdf)
    : IRequestHandler<ExportPrescriptionsPdfQuery, PdfReportResultDto>
{
    public async Task<PdfReportResultDto> Handle(ExportPrescriptionsPdfQuery request, CancellationToken ct)
    {
        var query = PrescriptionQueryFilters.Apply(
            ctx.Prescriptions.AsNoTracking(),
            request.Status,
            request.Search,
            request.PrescribedFrom,
            request.PrescribedTo,
            request.PatientSearch,
            request.DoctorSearch);

        PharmacySortValidator.ValidatePrescriptionSort(request.SortBy);
        query = PrescriptionQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var total = await query.CountAsync(ct);
        if (total > PharmacyExportLimits.MaxPdfExportRows)
            throw new eBolnicaBusinessRuleException(
                "export.limit_exceeded",
                $"PDF export is limited to {PharmacyExportLimits.MaxPdfExportRows} rows. Refine filters.");

        var summary = new PrescriptionsPdfSummary { TotalCount = total };

        var items = new List<PrescriptionEntity>(total);
        for (var skip = 0; skip < total; skip += PharmacyExportLimits.PdfFetchBatchSize)
        {
            var take = Math.Min(PharmacyExportLimits.PdfFetchBatchSize, total - skip);
            items.AddRange(await query.Skip(skip).Take(take).WithDetails().ToListAsync(ct));
        }

        var content = pdf.GeneratePrescriptionsPdf(summary, items);

        return new PdfReportResultDto
        {
            Content = content,
            FileName = $"prescriptions-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf",
            RowCount = total
        };
    }
}
