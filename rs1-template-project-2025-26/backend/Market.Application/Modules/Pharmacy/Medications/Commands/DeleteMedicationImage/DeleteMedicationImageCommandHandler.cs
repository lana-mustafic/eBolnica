using Market.Application.Abstractions;
using Market.Application.Modules.Pharmacy.Medications.Commands.DeleteMedicationImage;

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
            throw new MarketNotFoundException("Medication not found.");

        var image = medication.Images.FirstOrDefault(i => i.Id == request.ImageId && !i.IsDeleted);
        if (image is null)
            throw new MarketNotFoundException("Image not found.");

        await storage.DeleteAsync(image.RelativeUrl, ct);
        ctx.MedicationImages.Remove(image);

        if (image.IsPrimary)
        {
            var next = medication.Images.Where(i => i.Id != image.Id && !i.IsDeleted).OrderBy(i => i.SortOrder).FirstOrDefault();
            if (next is not null)
            {
                next.IsPrimary = true;
                medication.ImageUrl = next.RelativeUrl;
            }
            else medication.ImageUrl = null;
        }

        medication.ModifiedAtUtc = DateTime.UtcNow;
        await ctx.SaveChangesAsync(ct);
    }
}
