namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePharmacyPrescription;

public sealed class CreatePharmacyPrescriptionCommandValidator : AbstractValidator<CreatePharmacyPrescriptionCommand>
{
    public CreatePharmacyPrescriptionCommandValidator()
    {
        RuleFor(x => x.MedicalReportId).GreaterThan(0);
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.PrescriptionItems).NotEmpty();
        RuleForEach(x => x.PrescriptionItems).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicationId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 1000);
            item.RuleFor(i => i.Instructions).MaximumLength(500);
        });
    }
}
