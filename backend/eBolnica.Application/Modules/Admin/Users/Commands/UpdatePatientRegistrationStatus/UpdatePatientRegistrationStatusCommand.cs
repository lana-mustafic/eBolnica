namespace eBolnica.Application.Modules.Admin.Users.Commands.UpdatePatientRegistrationStatus;

public sealed class UpdatePatientRegistrationStatusCommand : IRequest<Common.MessageResponseDto>
{
    public int AppUserId { get; init; }
    public string RegistrationStatus { get; init; } = string.Empty;
}
