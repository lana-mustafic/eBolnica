using eBolnica.Domain.Common;
using eBolnica.Domain.Entities.Clinical;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class PharmacyInvoiceEntity : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int PrescriptionId { get; set; }
    public PrescriptionEntity Prescription { get; set; } = null!;
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;
    public int PharmacistId { get; set; }
    public PharmacistEntity Pharmacist { get; set; } = null!;
    public DateTime IssuedAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = PharmacyInvoiceStatuses.Issued;
    public ICollection<PharmacyInvoiceItemEntity> Items { get; set; } = new List<PharmacyInvoiceItemEntity>();
}

public static class PharmacyInvoiceStatuses
{
    public const string Issued = "Issued";
}
