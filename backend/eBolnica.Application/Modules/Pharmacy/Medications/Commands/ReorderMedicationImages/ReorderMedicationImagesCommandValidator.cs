namespace eBolnica.Application.Modules.Pharmacy.Medications.Commands.ReorderMedicationImages;

public sealed class ReorderMedicationImagesCommandValidator : AbstractValidator<ReorderMedicationImagesCommand>
{
    public ReorderMedicationImagesCommandValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0);
        RuleFor(x => x.ImageIds).NotNull().WithMessage("ImageIds is required.");
    }
}
