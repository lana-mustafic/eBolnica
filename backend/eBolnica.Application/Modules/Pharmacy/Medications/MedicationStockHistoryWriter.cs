using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Medications;

public static class MedicationStockHistoryWriter
{
    public static void Record(
        IAppDbContext ctx,
        int medicationId,
        int previousStock,
        int newStock,
        string reason,
        int? prescriptionId = null)
    {
        var change = newStock - previousStock;
        if (change == 0
            && reason != MedicationStockChangeReasons.InitialStock
            && reason != MedicationStockChangeReasons.Import)
        {
            return;
        }

        ctx.MedicationStockHistory.Add(new MedicationStockHistoryEntity
        {
            MedicationId = medicationId,
            ChangeQuantity = change,
            StockAfter = newStock,
            Reason = reason,
            PrescriptionId = prescriptionId,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
