namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationById;

public sealed class GetMedicationByIdQuery : IRequest<MedicationDto>
{
    public int Id { get; init; }
}
