using Market.Application.Abstractions;
using Market.Application.Common.Exceptions;
using Market.Domain.Entities.Pharmacy;
using Microsoft.AspNetCore.Hosting;

namespace Market.Infrastructure.Pharmacy;

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
            throw new MarketBusinessRuleException("validation.failed", "Allowed image types: JPG, PNG, WEBP.");

        if (content.CanSeek && content.Length > MaxFileSizeBytes)
            throw new MarketBusinessRuleException("validation.failed", "Image must be 5 MB or smaller.");

        var folder = Path.Combine(env.ContentRootPath, "uploads", "medications", medicationId.ToString());
        Directory.CreateDirectory(folder);

        var storedName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, storedName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        var fileInfo = new FileInfo(fullPath);
        var relativeUrl = $"/uploads/medications/{medicationId}/{storedName}";
        return (relativeUrl, fileInfo.Length);
    }

    public Task DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith("/uploads/", StringComparison.Ordinal))
            return Task.CompletedTask;

        var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(env.ContentRootPath, relativePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
