namespace eBolnica.Application.Modules.Admin.Users.Commands.CreateUser;

public sealed class CreateUserCommand : IRequest<Common.MessageResponseDto>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string UserType { get; init; } = string.Empty;
    public int? DoctorId { get; init; }
    public string? LicenseNumber { get; init; }
}
