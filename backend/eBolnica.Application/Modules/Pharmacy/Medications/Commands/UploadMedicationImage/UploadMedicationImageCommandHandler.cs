using eBolnica.Application.Abstractions;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.UploadMedicationImage;
using eBolnica.Application.Modules.Pharmacy.Medications.Images;
using eBolnica.Domain.Entities.Pharmacy;

public sealed class UploadMedicationImageCommandHandler(
    IAppDbContext ctx,
    IMedicationImageStorage storage)
    : IRequestHandler<UploadMedicationImageCommand, MedicationImageDto>
{
    public async Task<MedicationImageDto> Handle(UploadMedicationImageCommand request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .Include(m => m.Images)
            .FirstOrDefaultAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);

        if (medication is null)
            throw new eBolnicaNotFoundException("Medication not found.");

        var (relativeUrl, size) = await storage.SaveAsync(
            request.MedicationId, request.FileName, request.Content, ct);

        var isFirst = medication.Images.Count == 0;
        var image = new MedicationImageEntity
        {
            MedicationId = medication.Id,
            FileName = Path.GetFileName(request.FileName),
            RelativeUrl = relativeUrl,
            IsPrimary = isFirst,
            SortOrder = medication.Images.Count,
            FileSizeBytes = size,
            CreatedAtUtc = DateTime.UtcNow
        };

        ctx.MedicationImages.Add(image);

        if (isFirst)
            medication.ImageUrl = relativeUrl;

        medication.ModifiedAtUtc = DateTime.UtcNow;
        await ctx.SaveChangesAsync(ct);

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
}
