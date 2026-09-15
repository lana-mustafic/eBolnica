using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class PharmacyInvoiceItemEntity : BaseEntity
{
    public int InvoiceId { get; set; }
    public PharmacyInvoiceEntity Invoice { get; set; } = null!;
    public int MedicationId { get; set; }
    public MedicationEntity Medication { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
