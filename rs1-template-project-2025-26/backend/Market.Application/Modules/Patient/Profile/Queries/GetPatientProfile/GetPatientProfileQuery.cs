namespace Market.Application.Modules.Patient.Profile.Queries.GetPatientProfile;



public sealed class GetPatientProfileQuery : IRequest<GetPatientProfileQueryDto>

{

}



public sealed class GetPatientProfileQueryDto

{

    public int Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTime? DateOfBirth { get; init; }

    public string? Gender { get; init; }

    public string? PhoneNumber { get; init; }

    public string? Address { get; init; }

    public string? BloodType { get; init; }

    public string RegistrationStatus { get; init; } = string.Empty;

    public bool? IsAdmitted { get; init; }

    public string? RecordNumber { get; init; }

    public string? DoctorFirstName { get; init; }

    public string? DoctorLastName { get; init; }

    public string? DoctorSpecialization { get; init; }

}


