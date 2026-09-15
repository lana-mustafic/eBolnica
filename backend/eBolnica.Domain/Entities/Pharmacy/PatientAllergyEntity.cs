using eBolnica.Domain.Common;
using eBolnica.Domain.Entities.Clinical;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class PatientAllergyEntity : BaseEntity
{
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;
    public string Allergen { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public int? MedicationId { get; set; }
    public MedicationEntity? Medication { get; set; }
    public string? Notes { get; set; }
}
