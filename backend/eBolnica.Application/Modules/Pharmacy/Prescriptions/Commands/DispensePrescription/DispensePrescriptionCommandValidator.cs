namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.DispensePrescription;

public sealed class DispensePrescriptionCommandValidator : AbstractValidator<DispensePrescriptionCommand>
{
    public DispensePrescriptionCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).GreaterThan(0);
    }
}
