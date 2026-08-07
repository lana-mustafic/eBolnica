using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class PharmacyActivityEntity : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public int? PrescriptionId { get; set; }
    public PrescriptionEntity? Prescription { get; set; }
    public int? MedicationId { get; set; }
    public MedicationEntity? Medication { get; set; }
}
