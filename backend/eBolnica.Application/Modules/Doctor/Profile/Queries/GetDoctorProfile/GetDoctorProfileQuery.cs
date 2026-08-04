namespace eBolnica.Application.Modules.Doctor.Profile.Queries.GetDoctorProfile;

public sealed class GetDoctorProfileQuery : IRequest<GetDoctorProfileQueryDto>
{
}

public sealed class GetDoctorProfileQueryDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Specialization { get; init; }
    public string LicenseNumber { get; init; } = string.Empty;
    public DateTime? BirthDate { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
