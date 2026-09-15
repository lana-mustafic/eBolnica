using eBolnica.Domain.Common;
using eBolnica.Domain.Entities.Clinical;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class PurchaseOrderEntity : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public MedicationSupplierEntity Supplier { get; set; } = null!;
    public int PharmacistId { get; set; }
    public PharmacistEntity Pharmacist { get; set; } = null!;
    public string Status { get; set; } = PurchaseOrderStatuses.Ordered;
    public DateTime OrderedAtUtc { get; set; }
    public DateTime? ExpectedAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public ICollection<PurchaseOrderItemEntity> Items { get; set; } = new List<PurchaseOrderItemEntity>();
    public ICollection<StockReceiptEntity> Receipts { get; set; } = new List<StockReceiptEntity>();
}

public static class PurchaseOrderStatuses
{
    public const string Ordered = "Ordered";
    public const string Received = "Received";
    public const string Cancelled = "Cancelled";
}
