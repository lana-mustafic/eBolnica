using eBolnica.Application.Modules.Pharmacy.Medications.Commands.SetPrimaryMedicationImage;

public sealed class SetPrimaryMedicationImageCommandHandler(IAppDbContext ctx)
    : IRequestHandler<SetPrimaryMedicationImageCommand>
{
    public async Task Handle(SetPrimaryMedicationImageCommand request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .Include(m => m.Images)
            .FirstOrDefaultAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);

        if (medication is null)
            throw new eBolnicaNotFoundException("Medication not found.");

        var target = medication.Images.FirstOrDefault(i => i.Id == request.ImageId && !i.IsDeleted);
        if (target is null)
            throw new eBolnicaNotFoundException("Image not found.");

        foreach (var img in medication.Images.Where(i => !i.IsDeleted))
            img.IsPrimary = img.Id == target.Id;

        medication.ImageUrl = target.RelativeUrl;
        medication.ModifiedAtUtc = DateTime.UtcNow;
        await ctx.SaveChangesAsync(ct);
    }
}
