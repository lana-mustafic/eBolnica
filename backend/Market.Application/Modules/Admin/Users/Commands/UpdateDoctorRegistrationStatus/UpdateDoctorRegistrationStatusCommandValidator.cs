namespace Market.Application.Modules.Admin.Users.Commands.UpdateDoctorRegistrationStatus;

public sealed class UpdateDoctorRegistrationStatusCommandValidator
    : AbstractValidator<UpdateDoctorRegistrationStatusCommand>
{
    private static readonly string[] Allowed = ["Pending", "Approved", "Rejected"];

    public UpdateDoctorRegistrationStatusCommandValidator()
    {
        RuleFor(x => x.AppUserId).GreaterThan(0);
        RuleFor(x => x.RegistrationStatus)
            .NotEmpty()
            .Must(s => Allowed.Contains(s))
            .WithMessage("Registration status must be Pending, Approved, or Rejected.");
    }
}
