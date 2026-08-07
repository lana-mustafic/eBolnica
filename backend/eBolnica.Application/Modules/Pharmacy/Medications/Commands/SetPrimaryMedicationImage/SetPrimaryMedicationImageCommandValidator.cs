namespace eBolnica.Application.Modules.Pharmacy.Medications.Commands.SetPrimaryMedicationImage;

public sealed class SetPrimaryMedicationImageCommandValidator : AbstractValidator<SetPrimaryMedicationImageCommand>
{
    public SetPrimaryMedicationImageCommandValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0);
        RuleFor(x => x.ImageId).GreaterThan(0);
    }
}
