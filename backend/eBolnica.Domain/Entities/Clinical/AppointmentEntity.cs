using eBolnica.Domain.Common;
using eBolnica.Domain.Entities.Identity;

namespace eBolnica.Domain.Entities.Clinical;

public sealed class AppointmentEntity : BaseEntity
{
    public int PatientId { get; set; }
    public PatientEntity Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public DoctorEntity Doctor { get; set; } = null!;
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = AppointmentStatuses.Scheduled;
    public string? Notes { get; set; }
}

public static class AppointmentStatuses
{
    public const string Scheduled = "Scheduled";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}
