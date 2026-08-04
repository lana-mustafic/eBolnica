using Market.Domain.Common;

namespace Market.Domain.Entities.Clinical;

public sealed class MedicalRecordEntity : BaseEntity
{
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;
    public string RecordNumber { get; set; } = string.Empty;
}
