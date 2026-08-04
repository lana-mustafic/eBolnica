namespace eBolnica.Application.Modules.Admin.Users.Commands.UpdatePatientRegistrationStatus;

public sealed class UpdatePatientRegistrationStatusCommandValidator
    : AbstractValidator<UpdatePatientRegistrationStatusCommand>
{
    private static readonly string[] Allowed = ["Pending", "Approved", "Rejected"];

    public UpdatePatientRegistrationStatusCommandValidator()
    {
        RuleFor(x => x.AppUserId).GreaterThan(0);
        RuleFor(x => x.RegistrationStatus)
            .NotEmpty()
            .Must(s => Allowed.Contains(s))
            .WithMessage("Registration status must be Pending, Approved, or Rejected.");
    }
}
