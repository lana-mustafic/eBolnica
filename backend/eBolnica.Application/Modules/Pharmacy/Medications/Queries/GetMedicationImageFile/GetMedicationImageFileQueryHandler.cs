namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationImageFile;

public sealed class GetMedicationImageFileQueryHandler(IAppDbContext ctx, IMedicationImageStorage storage)
    : IRequestHandler<GetMedicationImageFileQuery, MedicationImageFileQueryDto?>
{
    public async Task<MedicationImageFileQueryDto?> Handle(GetMedicationImageFileQuery request, CancellationToken ct)
    {
        var medicationExists = await ctx.Medications
            .AsNoTracking()
            .AnyAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);

        if (!medicationExists)
            return null;

        var image = await ctx.MedicationImages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.Id == request.ImageId
                     && i.MedicationId == request.MedicationId
                     && !i.IsDeleted,
                ct);

        if (image is null)
            return null;

        var file = await storage.OpenReadAsync(image.RelativeUrl, ct);
        if (file is null)
            return null;

        return new MedicationImageFileQueryDto
        {
            FullPath = file.FullPath,
            ContentType = file.ContentType,
            FileName = file.FileName
        };
    }
}
