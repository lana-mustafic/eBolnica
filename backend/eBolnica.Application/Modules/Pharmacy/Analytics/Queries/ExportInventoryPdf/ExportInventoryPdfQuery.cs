using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Application.Modules.Pharmacy.Analytics.Queries.ExportInventoryPdf;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Analytics.Queries.ExportInventoryPdf;

public sealed class ExportInventoryPdfQuery : IRequest<PdfReportResultDto>
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeInactive { get; set; }
    public string? StockStatus { get; set; }
    public bool? RequiresPrescription { get; set; }
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
            request.StockStatus,
            request.RequiresPrescription);

        PharmacySortValidator.ValidateMedicationSort(request.SortBy);
        query = MedicationQueryFilters.ApplySorting(query, request.SortBy, request.SortOrder);

        var total = await query.CountAsync(ct);
        if (total > PharmacyExportLimits.MaxPdfExportRows)
            throw new eBolnicaBusinessRuleException(
                "export.limit_exceeded",
                $"PDF export is limited to {PharmacyExportLimits.MaxPdfExportRows} rows. Refine filters or use CSV export.");

        var generatedAt = DateTime.UtcNow;
        var horizon = generatedAt.AddDays(30);

        var summary = new InventoryPdfSummary
        {
            TotalCount = total,
            LowStockCount = await query.CountAsync(
                m => m.IsActive && m.StockQuantity > 0 && m.StockQuantity < m.MinimumStockLevel,
                ct),
            OutOfStockCount = await query.CountAsync(
                m => m.IsActive && m.StockQuantity <= 0,
                ct),
            ExpiringSoonCount = await query.CountAsync(
                m => m.ExpiryDate != null && m.ExpiryDate >= generatedAt && m.ExpiryDate <= horizon,
                ct)
        };

        var items = new List<MedicationEntity>(total);
        for (var skip = 0; skip < total; skip += PharmacyExportLimits.PdfFetchBatchSize)
        {
            var take = Math.Min(PharmacyExportLimits.PdfFetchBatchSize, total - skip);
            items.AddRange(await query.Skip(skip).Take(take).ToListAsync(ct));
        }

        var content = pdf.GenerateInventoryPdf(summary, items);

        return new PdfReportResultDto
        {
            Content = content,
            FileName = $"inventory-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf",
            RowCount = total
        };
    }
}
