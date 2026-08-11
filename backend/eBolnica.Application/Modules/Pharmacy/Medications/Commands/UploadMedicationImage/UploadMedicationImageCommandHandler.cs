using System.Data;
using eBolnica.Application.Abstractions;
using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.UploadMedicationImage;
using eBolnica.Application.Modules.Pharmacy.Medications.Images;
using eBolnica.Domain.Entities.Pharmacy;
using Microsoft.Extensions.Logging;

public sealed class UploadMedicationImageCommandHandler(
    IAppDbContext ctx,
    IMedicationImageStorage storage,
    ILogger<UploadMedicationImageCommandHandler> logger)
    : IRequestHandler<UploadMedicationImageCommand, MedicationImageDto>
{
    public async Task<MedicationImageDto> Handle(UploadMedicationImageCommand request, CancellationToken ct)
    {
        var medicationExists = await ctx.Medications
            .AsNoTracking()
            .AnyAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);

        if (!medicationExists)
            throw new eBolnicaNotFoundException("Medication not found.");

        var activeImageCount = await ctx.MedicationImages
            .CountAsync(i => i.MedicationId == request.MedicationId && !i.IsDeleted, ct);

        if (activeImageCount >= MedicationImageLimits.MaxPerMedication)
        {
            throw new eBolnicaBusinessRuleException(
                "medication.image_limit_reached",
                $"A medication can have at most {MedicationImageLimits.MaxPerMedication} images.");
        }

        string? relativeUrl = null;
        long fileSize;

        try
        {
            var (savedUrl, size) = await storage.SaveAsync(
                request.MedicationId, request.FileName, request.Content, ct);
            relativeUrl = savedUrl;
            fileSize = size;
        }
        catch
        {
            throw;
        }

        await using var transaction = await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var medication = await ctx.Medications
                .Include(m => m.Images)
                .FirstOrDefaultAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);

            if (medication is null)
                throw new eBolnicaNotFoundException("Medication not found.");

            var activeImages = medication.Images.Where(i => !i.IsDeleted).ToList();
            if (activeImages.Count >= MedicationImageLimits.MaxPerMedication)
            {
                throw new eBolnicaBusinessRuleException(
                    "medication.image_limit_reached",
                    $"A medication can have at most {MedicationImageLimits.MaxPerMedication} images.");
            }

            var isFirst = activeImages.Count == 0;
            var image = new MedicationImageEntity
            {
                MedicationId = medication.Id,
                FileName = Path.GetFileName(request.FileName),
                RelativeUrl = relativeUrl,
                IsPrimary = isFirst,
                SortOrder = activeImages.Count,
                FileSizeBytes = fileSize,
                CreatedAtUtc = DateTime.UtcNow
            };

            ctx.MedicationImages.Add(image);

            if (isFirst)
                medication.ImageUrl = relativeUrl;

            medication.ModifiedAtUtc = DateTime.UtcNow;
            await ctx.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            PharmacyOperationLogger.MedicationImageUploaded(
                logger,
                medication.Id,
                image.Id,
                image.FileName);

            return new MedicationImageDto
            {
                Id = image.Id,
                MedicationId = image.MedicationId,
                FileName = image.FileName,
                RelativeUrl = image.RelativeUrl,
                IsPrimary = image.IsPrimary,
                SortOrder = image.SortOrder,
                FileSizeBytes = image.FileSizeBytes
            };
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            if (relativeUrl is not null)
            {
                try
                {
                    await storage.DeleteAsync(relativeUrl, ct);
                }
                catch
                {
                    // Best-effort cleanup of orphaned file.
                }
            }

            throw;
        }
    }
}
