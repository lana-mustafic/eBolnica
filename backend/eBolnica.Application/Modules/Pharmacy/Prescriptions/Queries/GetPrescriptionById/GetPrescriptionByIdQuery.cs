namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.GetPrescriptionById;

public sealed class GetPrescriptionByIdQuery : IRequest<PrescriptionDto>
{
    public int Id { get; set; }
}
