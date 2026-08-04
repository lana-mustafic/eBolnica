namespace eBolnica.Application.Modules.Admin.Users.Commands.DeleteUser;

public sealed class DeleteUserCommand : IRequest<Common.MessageResponseDto>
{
    public int AppUserId { get; init; }
}
