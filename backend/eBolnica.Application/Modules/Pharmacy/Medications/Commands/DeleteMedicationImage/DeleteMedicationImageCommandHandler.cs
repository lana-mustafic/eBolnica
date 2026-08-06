using eBolnica.Application.Abstractions;
using eBolnica.Application.Modules.Pharmacy.Medications.Commands.DeleteMedicationImage;

public sealed class DeleteMedicationImageCommandHandler(
    IAppDbContext ctx,
    IMedicationImageStorage storage)
    : IRequestHandler<DeleteMedicationImageCommand>
{
    public async Task Handle(DeleteMedicationImageCommand request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .Include(m => m.Images)
            .FirstOrDefaultAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);

        if (medication is null)
            throw new eBolnicaNotFoundException("Medication not found.");

        var image = medication.Images.FirstOrDefault(i => i.Id == request.ImageId && !i.IsDeleted);
        if (image is null)
            throw new eBolnicaNotFoundException("Image not found.");

        var relativeUrl = image.RelativeUrl;
        var wasPrimary = image.IsPrimary;
        var now = DateTime.UtcNow;

        image.IsDeleted = true;
        image.ModifiedAtUtc = now;

        if (wasPrimary)
        {
            var next = medication.Images
                .Where(i => i.Id != image.Id && !i.IsDeleted)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefault();

            if (next is not null)
            {
                next.IsPrimary = true;
                medication.ImageUrl = next.RelativeUrl;
            }
            else
            {
                medication.ImageUrl = null;
            }
        }

        medication.ModifiedAtUtc = now;
        await ctx.SaveChangesAsync(ct);

        try
        {
            await storage.DeleteAsync(relativeUrl, ct);
        }
        catch
        {
            // DB state is authoritative; file cleanup can be retried later.
        }
    }
}
