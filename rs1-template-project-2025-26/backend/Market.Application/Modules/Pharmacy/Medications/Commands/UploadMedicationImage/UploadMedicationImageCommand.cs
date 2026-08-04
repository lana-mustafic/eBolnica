using Market.Application.Modules.Pharmacy.Medications.Images;

namespace Market.Application.Modules.Pharmacy.Medications.Commands.UploadMedicationImage;

public sealed class UploadMedicationImageCommand : IRequest<MedicationImageDto>
{
    public int MedicationId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public Stream Content { get; init; } = Stream.Null;
}
