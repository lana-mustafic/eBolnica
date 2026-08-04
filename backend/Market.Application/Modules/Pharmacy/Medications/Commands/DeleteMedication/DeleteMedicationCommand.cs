namespace Market.Application.Modules.Pharmacy.Medications.Commands.DeleteMedication;

public sealed class DeleteMedicationCommand : IRequest
{
    public int Id { get; init; }
}
