using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class MedicationSupplierEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<PurchaseOrderEntity> PurchaseOrders { get; set; } = new List<PurchaseOrderEntity>();
}
