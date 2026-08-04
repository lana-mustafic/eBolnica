using Market.Domain.Common;

namespace Market.Domain.Entities.Pharmacy;

public sealed class PrescriptionItemEntity : BaseEntity
{
    public int PrescriptionId { get; set; }
    public PrescriptionEntity Prescription { get; set; } = null!;
    public int MedicationId { get; set; }
    public MedicationEntity Medication { get; set; } = null!;
    public int Quantity { get; set; }
    public string? Instructions { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
