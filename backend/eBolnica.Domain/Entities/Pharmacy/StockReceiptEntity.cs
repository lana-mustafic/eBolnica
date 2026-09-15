using eBolnica.Domain.Common;
using eBolnica.Domain.Entities.Clinical;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class StockReceiptEntity : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public int PurchaseOrderId { get; set; }
    public PurchaseOrderEntity PurchaseOrder { get; set; } = null!;
    public int PharmacistId { get; set; }
    public PharmacistEntity Pharmacist { get; set; } = null!;
    public DateTime ReceivedAtUtc { get; set; }
    public string? Notes { get; set; }
    public ICollection<StockReceiptItemEntity> Items { get; set; } = new List<StockReceiptItemEntity>();
}
