using eBolnica.Application.Modules.Pharmacy.Prescriptions;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;

public sealed class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator()
    {
        PrescriptionCreateValidationRules.Apply(
            this,
            x => x.MedicalReportId,
            x => x.PatientId,
            x => x.Notes,
            x => x.PrescriptionItems);
    }
}
