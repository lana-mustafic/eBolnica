using Market.Domain.Common;

namespace Market.Domain.Entities.Pharmacy;

public sealed class MedicationImageEntity : BaseEntity
{
    public int MedicationId { get; set; }
    public MedicationEntity Medication { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string RelativeUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public long? FileSizeBytes { get; set; }
}
