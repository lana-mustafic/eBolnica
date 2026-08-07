using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class MedicationStockHistoryEntity : BaseEntity
{
    public int MedicationId { get; set; }
    public MedicationEntity Medication { get; set; } = null!;
    public int ChangeQuantity { get; set; }
    public int StockAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? PrescriptionId { get; set; }
    public PrescriptionEntity? Prescription { get; set; }
}
