namespace Market.Application.Modules.Auth.Commands.RegisterPatient;

public sealed class RegisterPatientCommand : IRequest<RegisterPatientCommandDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

public sealed class RegisterPatientCommandDto
{
    public string Message { get; set; } = "Patient registered successfully";
}
