namespace eBolnica.Application.Modules.MedicalRecord.Queries.GetMedicalRecordByPatientId;

public sealed class GetMedicalRecordByPatientIdQuery : IRequest<GetMedicalRecordByPatientIdQueryDto>
{
    public int PatientId { get; init; }
}

public sealed class GetMedicalRecordByPatientIdQueryDto
{
    public int Id { get; init; }
    public int PatientId { get; init; }
    public string RecordNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public bool? IsAdmitted { get; init; }
    public string? BloodType { get; init; }
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<MedicalReportItemDto> Reports { get; init; } = Array.Empty<MedicalReportItemDto>();
    public IReadOnlyList<AppointmentItemDto> Appointments { get; init; } = Array.Empty<AppointmentItemDto>();
    public IReadOnlyList<HospitalizationItemDto> Hospitalizations { get; init; } = Array.Empty<HospitalizationItemDto>();
    public IReadOnlyList<ClinicalDiagnosisItemDto> Diagnoses { get; init; } = Array.Empty<ClinicalDiagnosisItemDto>();
    public IReadOnlyList<PatientAllergyItemDto> Allergies { get; init; } = Array.Empty<PatientAllergyItemDto>();
}

public sealed class AppointmentItemDto
{
    public int Id { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public int DurationMinutes { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public sealed class HospitalizationItemDto
{
    public int Id { get; init; }
    public DateTime AdmittedAtUtc { get; init; }
    public DateTime? DischargedAtUtc { get; init; }
    public string Ward { get; init; } = string.Empty;
    public string? RoomNumber { get; init; }
    public string AdmissionReason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class ClinicalDiagnosisItemDto
{
    public int Id { get; init; }
    public string? Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime DiagnosedAtUtc { get; init; }
    public int? MedicalReportId { get; init; }
}

public sealed class PatientAllergyItemDto
{
    public int Id { get; init; }
    public string Allergen { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string? Reaction { get; init; }
}

public sealed class MedicalReportItemDto
{
    public int Id { get; init; }
    public int DoctorId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Diagnosis { get; init; }
    public string? Therapy { get; init; }
    public string? Symptoms { get; init; }
    public string? Description { get; init; }
}
