namespace Market.Application.Abstractions;

public interface IMedicationImageStorage
{
    Task<(string RelativeUrl, long FileSizeBytes)> SaveAsync(
        int medicationId,
        string originalFileName,
        Stream content,
        CancellationToken ct = default);

    Task DeleteAsync(string relativeUrl, CancellationToken ct = default);
}
