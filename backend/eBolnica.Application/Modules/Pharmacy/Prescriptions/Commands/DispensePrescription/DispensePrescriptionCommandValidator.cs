namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.DispensePrescription;

public sealed class DispensePrescriptionCommandValidator : AbstractValidator<DispensePrescriptionCommand>
{
    public DispensePrescriptionCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).GreaterThan(0);
        RuleFor(x => x.DispensedDate)
            .Must(d => !d.HasValue || d.Value <= DateTime.UtcNow.AddDays(1))
            .WithMessage("Dispensed date cannot be more than one day in the future.");
        RuleFor(x => x.DispensedDate)
            .Must(d => !d.HasValue || d.Value >= DateTime.UtcNow.AddYears(-5))
            .WithMessage("Dispensed date is too far in the past.");
    }
}
