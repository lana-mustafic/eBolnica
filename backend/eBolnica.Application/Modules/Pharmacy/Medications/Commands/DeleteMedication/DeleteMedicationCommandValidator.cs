using eBolnica.Application.Modules.Pharmacy.Medications.Commands.DeleteMedication;

public sealed class DeleteMedicationCommandValidator : AbstractValidator<DeleteMedicationCommand>
{
    public DeleteMedicationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
