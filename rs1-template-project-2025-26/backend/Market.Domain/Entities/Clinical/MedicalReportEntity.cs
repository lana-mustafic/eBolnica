using Market.Domain.Common;

namespace Market.Domain.Entities.Clinical;

public sealed class MedicalReportEntity : BaseEntity
{
    public int MedicalRecordId { get; set; }
    public MedicalRecordEntity MedicalRecord { get; set; } = null!;
    public int DoctorId { get; set; }
    public DoctorEntity Doctor { get; set; } = null!;
    public string? Diagnosis { get; set; }
    public string? Symptoms { get; set; }
    public string? Therapy { get; set; }
    public string? Description { get; set; }
}
