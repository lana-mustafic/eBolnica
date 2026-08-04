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
}

public sealed class MedicalReportItemDto
{
    public int DoctorId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Diagnosis { get; init; }
    public string? Therapy { get; init; }
    public string? Symptoms { get; init; }
    public string? Description { get; init; }
}
