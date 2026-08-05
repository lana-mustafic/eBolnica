namespace eBolnica.Application.Abstractions;

public interface IMedicationImageStorage
{
    Task<(string RelativeUrl, long FileSizeBytes)> SaveAsync(
        int medicationId,
        string originalFileName,
        Stream content,
        CancellationToken ct = default);

    Task DeleteAsync(string relativeUrl, CancellationToken ct = default);

    Task<MedicationImageFileResult?> OpenReadAsync(string relativeUrl, CancellationToken ct = default);
}

public sealed class MedicationImageFileResult
{
    public string FullPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public string FileName { get; init; } = string.Empty;
}
