namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.ListPatientMedicalReportsForPrescription;

public sealed class ListPatientMedicalReportsForPrescriptionQueryValidator
    : AbstractValidator<ListPatientMedicalReportsForPrescriptionQuery>
{
    public ListPatientMedicalReportsForPrescriptionQueryValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
    }
}
