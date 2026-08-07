using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationStockHistory;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationStockHistory;

public sealed class GetMedicationStockHistoryQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetMedicationStockHistoryQuery, IReadOnlyList<MedicationStockHistoryDto>>
{
    public async Task<IReadOnlyList<MedicationStockHistoryDto>> Handle(
        GetMedicationStockHistoryQuery request,
        CancellationToken ct)
    {
        var exists = await ctx.Medications.AnyAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);
        if (!exists)
            throw new eBolnicaNotFoundException("Medication not found.");

        var limit = Math.Clamp(request.Limit, 1, 200);

        return await ctx.MedicationStockHistory
            .AsNoTracking()
            .Where(h => h.MedicationId == request.MedicationId && !h.IsDeleted)
            .OrderByDescending(h => h.CreatedAtUtc)
            .ThenByDescending(h => h.Id)
            .Take(limit)
            .Select(h => new MedicationStockHistoryDto
            {
                Id = h.Id,
                OccurredAt = h.CreatedAtUtc,
                ChangeQuantity = h.ChangeQuantity,
                StockAfter = h.StockAfter,
                Reason = h.Reason,
                ReferenceLabel = h.Prescription != null ? h.Prescription.PrescriptionNumber : null
            })
            .ToListAsync(ct);
    }
}
