namespace Market.Application.Modules.Auth.Commands.RegisterDoctor;

public sealed class RegisterDoctorCommandValidator : AbstractValidator<RegisterDoctorCommand>
{
    public RegisterDoctorCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("The password and confirmation password do not match.");
        RuleFor(x => x.Gender).MaximumLength(20).When(x => x.Gender is not null);
    }
}
