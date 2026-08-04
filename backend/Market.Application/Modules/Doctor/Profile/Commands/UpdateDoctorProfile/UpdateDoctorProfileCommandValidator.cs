using Market.Application.Modules.Doctor.Profile.Commands.UpdateDoctorProfile;

public sealed class UpdateDoctorProfileCommandValidator : AbstractValidator<UpdateDoctorProfileCommand>
{
    public UpdateDoctorProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Specialization).MaximumLength(100);
    }
}
