namespace Market.Application.Modules.Pharmacy.Medications.Commands.SetPrimaryMedicationImage;

public sealed class SetPrimaryMedicationImageCommand : IRequest
{
    public int MedicationId { get; init; }
    public int ImageId { get; init; }
}
