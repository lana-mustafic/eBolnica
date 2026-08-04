namespace Market.Application.Modules.Pharmacy.Prescriptions.Commands.DispensePrescription;

using Market.Application.Modules.Pharmacy.Prescriptions;

public sealed class DispensePrescriptionCommand : IRequest<PrescriptionDto>
{
    public int PrescriptionId { get; set; }
    public DateTime? DispensedDate { get; set; }
}
