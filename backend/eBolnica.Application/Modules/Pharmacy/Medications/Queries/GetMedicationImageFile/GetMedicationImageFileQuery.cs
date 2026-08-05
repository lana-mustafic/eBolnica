namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationImageFile;

public sealed class GetMedicationImageFileQuery : IRequest<MedicationImageFileQueryDto?>
{
    public int MedicationId { get; init; }
    public int ImageId { get; init; }
}

public sealed class MedicationImageFileQueryDto
{
    public string FullPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public string FileName { get; init; } = string.Empty;
}
