namespace eBolnica.Application.Modules.Pharmacy.Medications.Commands.DeleteMedicationImage;

public sealed class DeleteMedicationImageCommandValidator : AbstractValidator<DeleteMedicationImageCommand>
{
    public DeleteMedicationImageCommandValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0);
        RuleFor(x => x.ImageId).GreaterThan(0);
    }
}
