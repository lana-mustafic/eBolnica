namespace eBolnica.Application.Modules.Pharmacy.Medications.Commands.ReorderMedicationImages;

public sealed class ReorderMedicationImagesCommandHandler(IAppDbContext ctx)
    : IRequestHandler<ReorderMedicationImagesCommand>
{
    public async Task Handle(ReorderMedicationImagesCommand request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .Include(m => m.Images)
            .FirstOrDefaultAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);

        if (medication is null)
            throw new eBolnicaNotFoundException("Medication not found.");

        var activeImages = medication.Images.Where(i => !i.IsDeleted).ToList();
        var imageIds = request.ImageIds ?? Array.Empty<int>();
        if (imageIds.Count != activeImages.Count)
            throw new eBolnicaBusinessRuleException("image.reorder_invalid", "Image list must include all active images.");

        var activeIds = activeImages.Select(i => i.Id).ToHashSet();
        if (imageIds.Any(id => !activeIds.Contains(id)))
            throw new eBolnicaBusinessRuleException("image.reorder_invalid", "One or more images do not belong to this medication.");

        if (imageIds.Distinct().Count() != imageIds.Count)
            throw new eBolnicaBusinessRuleException("image.reorder_invalid", "Duplicate image ids in reorder request.");

        for (var i = 0; i < imageIds.Count; i++)
        {
            var image = activeImages.First(img => img.Id == imageIds[i]);
            image.SortOrder = i;
        }

        medication.ModifiedAtUtc = DateTime.UtcNow;
        await ctx.SaveChangesAsync(ct);
    }
}
