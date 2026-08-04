using eBolnica.Application.Modules.Pharmacy.Medications.Images;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedicationImages;

public sealed class ListMedicationImagesQuery : IRequest<IReadOnlyList<MedicationImageDto>>
{
    public int MedicationId { get; init; }
}
