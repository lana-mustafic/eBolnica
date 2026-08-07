using eBolnica.Application.Modules.Pharmacy.Prescriptions;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePharmacyPrescription;

public sealed class CreatePharmacyPrescriptionCommandValidator : AbstractValidator<CreatePharmacyPrescriptionCommand>
{
    public CreatePharmacyPrescriptionCommandValidator()
    {
        PrescriptionCreateValidationRules.Apply(
            this,
            x => x.MedicalReportId,
            x => x.PatientId,
            x => x.Notes,
            x => x.PrescriptionItems);
    }
}
