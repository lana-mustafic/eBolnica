using eBolnica.Application.Abstractions;
using eBolnica.Application.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;

namespace eBolnica.Infrastructure.Pharmacy;

public sealed class MedicationImageStorageService(IWebHostEnvironment env) : IMedicationImageStorage
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public async Task<(string RelativeUrl, long FileSizeBytes)> SaveAsync(
        int medicationId,
        string originalFileName,
        Stream content,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new eBolnicaBusinessRuleException("validation.failed", "Allowed image types: JPG, PNG, WEBP.");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        if (bytes.Length == 0)
            throw new eBolnicaBusinessRuleException("validation.failed", "Uploaded image is empty.");

        if (bytes.Length > MaxFileSizeBytes)
            throw new eBolnicaBusinessRuleException("validation.failed", "Image must be 5 MB or smaller.");

        if (!MedicationImageContentValidator.IsSupportedImageContent(bytes, ext))
            throw new eBolnicaBusinessRuleException("validation.failed", "File content does not match a supported image format.");

        var folder = Path.Combine(env.ContentRootPath, "uploads", "medications", medicationId.ToString());
        Directory.CreateDirectory(folder);

        var storedName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, storedName);

        await File.WriteAllBytesAsync(fullPath, bytes, ct);

        var relativeUrl = $"/uploads/medications/{medicationId}/{storedName}";
        return (relativeUrl, bytes.Length);
    }

    public Task DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        var fullPath = ResolveSafeUploadPath(relativeUrl);
        if (fullPath is not null && File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<MedicationImageFileResult?> OpenReadAsync(string relativeUrl, CancellationToken ct = default)
    {
        var fullPath = ResolveSafeUploadPath(relativeUrl);
        if (fullPath is null || !File.Exists(fullPath))
            return Task.FromResult<MedicationImageFileResult?>(null);

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return Task.FromResult<MedicationImageFileResult?>(new MedicationImageFileResult
        {
            FullPath = fullPath,
            ContentType = contentType,
            FileName = Path.GetFileName(fullPath)
        });
    }

    private string? ResolveSafeUploadPath(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith("/uploads/", StringComparison.Ordinal))
            return null;

        var uploadsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "uploads"));
        var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, relativePath));

        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        return fullPath;
    }
}
