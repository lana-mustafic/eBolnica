namespace Market.Application.Modules.Admin.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.UserType)
            .NotEmpty()
            .Must(t => t is UserTypes.Doctor or UserTypes.Patient or UserTypes.Pharmacist)
            .WithMessage("UserType must be Doctor, Patient, or Pharmacist.");

        RuleFor(x => x.DoctorId)
            .NotNull()
            .When(x => x.UserType == UserTypes.Patient)
            .WithMessage("DoctorId is required for patient creation.");
    }
}
