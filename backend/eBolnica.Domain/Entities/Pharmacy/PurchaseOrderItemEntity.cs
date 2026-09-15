using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class PurchaseOrderItemEntity : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrderEntity PurchaseOrder { get; set; } = null!;
    public int MedicationId { get; set; }
    public MedicationEntity Medication { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
