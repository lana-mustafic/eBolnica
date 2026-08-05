namespace eBolnica.Application.Modules.Pharmacy.Medications.Commands.ReorderMedicationImages;

public sealed class ReorderMedicationImagesCommand : IRequest
{
    public int MedicationId { get; init; }
    public IReadOnlyList<int> ImageIds { get; init; } = Array.Empty<int>();
}
