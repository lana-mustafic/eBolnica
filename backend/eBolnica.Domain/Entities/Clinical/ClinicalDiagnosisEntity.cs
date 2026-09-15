using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Clinical;

public sealed class ClinicalDiagnosisEntity : BaseEntity
{
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public DoctorEntity Doctor { get; set; } = null!;
    public int? MedicalReportId { get; set; }
    public MedicalReportEntity? MedicalReport { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DiagnosedAtUtc { get; set; }
}
