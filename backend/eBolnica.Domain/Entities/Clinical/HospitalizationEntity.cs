using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Clinical;

public sealed class HospitalizationEntity : BaseEntity
{
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public DoctorEntity Doctor { get; set; } = null!;
    public DateTime AdmittedAtUtc { get; set; }
    public DateTime? DischargedAtUtc { get; set; }
    public string Ward { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public string AdmissionReason { get; set; } = string.Empty;
    public string Status { get; set; } = HospitalizationStatuses.Admitted;
    public string? Notes { get; set; }
}

public static class HospitalizationStatuses
{
    public const string Admitted = "Admitted";
    public const string Discharged = "Discharged";
}
