namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CancelPrescription;

public sealed class CancelPrescriptionCommandValidator : AbstractValidator<CancelPrescriptionCommand>
{
    public CancelPrescriptionCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).GreaterThan(0);
    }
}
