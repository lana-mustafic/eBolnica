namespace Market.Application.Modules.Pharmacy.Medications.Commands.DeleteMedicationImage;

public sealed class DeleteMedicationImageCommand : IRequest
{
    public int MedicationId { get; init; }
    public int ImageId { get; init; }
}
