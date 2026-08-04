namespace eBolnica.Application.Modules.Doctor.Patients.Queries.ListDoctorPatients;

public sealed class ListDoctorPatientsQuery : IRequest<ListDoctorPatientsQueryDto>
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Gender { get; init; }
    public string? BloodType { get; init; }
    public int? BirthYear { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public sealed class ListDoctorPatientsQueryDto
{
    public IReadOnlyList<DoctorAssignedPatientDto> Items { get; init; } = Array.Empty<DoctorAssignedPatientDto>();
    public int TotalCount { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public sealed class DoctorAssignedPatientDto
{
    public int Id { get; init; }
    public int DoctorId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? BloodType { get; init; }
    public string RecordNumber { get; init; } = string.Empty;
}
