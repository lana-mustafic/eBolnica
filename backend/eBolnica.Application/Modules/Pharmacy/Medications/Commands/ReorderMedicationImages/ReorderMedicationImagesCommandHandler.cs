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
        if (request.ImageIds.Count != activeImages.Count)
            throw new eBolnicaBusinessRuleException("image.reorder_invalid", "Image list must include all active images.");

        var activeIds = activeImages.Select(i => i.Id).ToHashSet();
        if (request.ImageIds.Any(id => !activeIds.Contains(id)))
            throw new eBolnicaBusinessRuleException("image.reorder_invalid", "One or more images do not belong to this medication.");

        if (request.ImageIds.Distinct().Count() != request.ImageIds.Count)
            throw new eBolnicaBusinessRuleException("image.reorder_invalid", "Duplicate image ids in reorder request.");

        for (var i = 0; i < request.ImageIds.Count; i++)
        {
            var image = activeImages.First(img => img.Id == request.ImageIds[i]);
            image.SortOrder = i;
        }

        medication.ModifiedAtUtc = DateTime.UtcNow;
        await ctx.SaveChangesAsync(ct);
    }
}
