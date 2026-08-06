namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePharmacyPrescription;

using eBolnica.Application.Modules.Pharmacy.Prescriptions;
using eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;

public sealed class CreatePharmacyPrescriptionCommand : IRequest<PrescriptionDto>
{
    public int MedicalReportId { get; set; }
    public int PatientId { get; set; }
    public string? Notes { get; set; }
    public List<CreatePrescriptionItemCommand> PrescriptionItems { get; set; } = new();
}
