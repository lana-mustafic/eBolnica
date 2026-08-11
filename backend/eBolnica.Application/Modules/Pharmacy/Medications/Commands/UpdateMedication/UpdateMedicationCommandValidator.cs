using eBolnica.Application.Modules.Pharmacy.Medications.Commands.UpdateMedication;

public sealed class UpdateMedicationCommandValidator : AbstractValidator<UpdateMedicationCommand>
{
    public UpdateMedicationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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
        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv!.Length > 0)
            .WithMessage("RowVersion is required for medication updates.");
        RuleFor(x => x)
            .Must(x => !x.IsActive || x.ExpiryDate.Date > DateTime.UtcNow.Date)
            .WithMessage("Expiry date must be in the future for active medications.");
    }
}
