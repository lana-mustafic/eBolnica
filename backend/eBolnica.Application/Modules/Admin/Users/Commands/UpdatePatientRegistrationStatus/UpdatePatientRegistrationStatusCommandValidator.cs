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
        RuleFor(x => x.DoctorId)
            .NotNull()
            .GreaterThan(0)
            .When(x => string.Equals(x.RegistrationStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            .WithMessage("An approved doctor must be assigned before the patient can be approved.");
    }
}
