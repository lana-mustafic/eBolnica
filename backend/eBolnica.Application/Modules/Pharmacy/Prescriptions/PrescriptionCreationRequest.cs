using eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

public sealed class PrescriptionCreationRequest
{
    public int MedicalReportId { get; init; }
    public int PatientId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<CreatePrescriptionItemCommand> PrescriptionItems { get; init; } = Array.Empty<CreatePrescriptionItemCommand>();
}
