using Market.Domain.Common;
using Market.Domain.Entities.Identity;

namespace Market.Domain.Entities.Clinical;

public sealed class PatientEntity : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public MarketUserEntity User { get; set; } = null!;
    public int? DoctorId { get; set; }
    public DoctorEntity? Doctor { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? BloodType { get; set; }
    public bool? IsAdmitted { get; set; }
    public string RegistrationStatus { get; set; } = "Pending";
    public MedicalRecordEntity? MedicalRecord { get; set; }
}
