namespace Market.Application.Modules.Auth.Commands.RegisterDoctor;

public sealed class RegisterDoctorCommand : IRequest<RegisterDoctorCommandDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

public sealed class RegisterDoctorCommandDto
{
    public string Message { get; set; } = "Doctor registered successfully";
}
