namespace eBolnica.Application.Modules.Doctor.Profile.Commands.UpdateDoctorProfile;

public sealed class UpdateDoctorProfileCommand : IRequest<UpdateDoctorProfileCommandDto>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Specialization { get; init; }
}

public sealed class UpdateDoctorProfileCommandDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Specialization { get; init; }
}
