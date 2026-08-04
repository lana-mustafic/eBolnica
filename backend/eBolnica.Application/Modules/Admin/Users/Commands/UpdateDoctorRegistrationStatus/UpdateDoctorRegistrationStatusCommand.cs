namespace eBolnica.Application.Modules.Admin.Users.Commands.UpdateDoctorRegistrationStatus;

public sealed class UpdateDoctorRegistrationStatusCommand : IRequest<Common.MessageResponseDto>
{
    public int AppUserId { get; init; }
    public string RegistrationStatus { get; init; } = string.Empty;
}
