namespace Market.Application.Modules.Admin.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.AppUserId).GreaterThan(0);
    }
}
