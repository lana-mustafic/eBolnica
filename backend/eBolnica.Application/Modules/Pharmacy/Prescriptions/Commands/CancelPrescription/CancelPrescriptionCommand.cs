namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CancelPrescription;

using eBolnica.Application.Modules.Pharmacy.Prescriptions;

public sealed class CancelPrescriptionCommand : IRequest<PrescriptionDto>
{
    public int PrescriptionId { get; set; }
}
