using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class StockReceiptItemEntity : BaseEntity
{
    public int StockReceiptId { get; set; }
    public StockReceiptEntity StockReceipt { get; set; } = null!;
    public int MedicationId { get; set; }
    public MedicationEntity Medication { get; set; } = null!;
    public int Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
