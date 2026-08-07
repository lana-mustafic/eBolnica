using eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

public sealed class PrescriptionCreateItemValidator : AbstractValidator<CreatePrescriptionItemCommand>
{
    public PrescriptionCreateItemValidator()
    {
        RuleFor(i => i.MedicationId).GreaterThan(0);
        RuleFor(i => i.Quantity).InclusiveBetween(1, 1000);
        RuleFor(i => i.Instructions).MaximumLength(500);
    }
}
