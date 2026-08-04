using eBolnica.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;

public sealed class CreateMedicationCommandValidator : AbstractValidator<CreateMedicationCommand>
{
    public CreateMedicationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.GenericName).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Manufacturer).MaximumLength(100);
        RuleFor(x => x.Price).InclusiveBetween(0.01m, 10000m);
        RuleFor(x => x.StockQuantity).InclusiveBetween(0, 10000);
        RuleFor(x => x.MinimumStockLevel).InclusiveBetween(0, 10000);
        RuleFor(x => x.BatchNumber).MaximumLength(50);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DosageForm).MaximumLength(50);
        RuleFor(x => x.Strength).MaximumLength(50);
        RuleFor(x => x.ExpiryDate)
            .Must(d => d.Date > DateTime.UtcNow.Date)
            .WithMessage("Expiry date must be in the future.");
    }
}
