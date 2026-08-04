namespace Market.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;

using Market.Application.Modules.Pharmacy.Prescriptions;

public sealed class CreatePrescriptionItemCommand
{
    public int MedicationId { get; set; }
    public int Quantity { get; set; }
    public string? Instructions { get; set; }
}

public sealed class CreatePrescriptionCommand : IRequest<PrescriptionDto>
{
    public int MedicalReportId { get; set; }
    public int PatientId { get; set; }
    public string? Notes { get; set; }
    public List<CreatePrescriptionItemCommand> PrescriptionItems { get; set; } = new();
}
