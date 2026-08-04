using Market.Domain.Common;
using Market.Domain.Entities.Identity;

namespace Market.Domain.Entities.Clinical;

public sealed class DoctorEntity : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public MarketUserEntity User { get; set; } = null!;
    public string RegistrationStatus { get; set; } = "Pending";
    public string? PhoneNumber { get; set; }
    public string? Specialization { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public ICollection<PatientEntity> Patients { get; private set; } = new List<PatientEntity>();
}
