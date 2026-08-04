namespace Market.Application.Modules.Pharmacy.Medications.Images;

public sealed class MedicationImageDto
{
    public int Id { get; init; }
    public int MedicationId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string RelativeUrl { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
    public long? FileSizeBytes { get; init; }
}
