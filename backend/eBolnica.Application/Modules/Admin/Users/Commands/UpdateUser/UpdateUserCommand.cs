namespace eBolnica.Application.Modules.Admin.Users.Commands.UpdateUser;

public sealed class UpdateUserCommand : IRequest<Common.MessageResponseDto>
{
    public int AppUserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
