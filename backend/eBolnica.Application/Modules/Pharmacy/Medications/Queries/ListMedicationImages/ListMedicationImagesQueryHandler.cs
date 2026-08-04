using eBolnica.Application.Modules.Pharmacy.Medications.Images;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedicationImages;

public sealed class ListMedicationImagesQueryHandler(IAppDbContext ctx)
    : IRequestHandler<ListMedicationImagesQuery, IReadOnlyList<MedicationImageDto>>
{
    public async Task<IReadOnlyList<MedicationImageDto>> Handle(ListMedicationImagesQuery request, CancellationToken ct)
    {
        var exists = await ctx.Medications.AnyAsync(m => m.Id == request.MedicationId && !m.IsDeleted, ct);
        if (!exists)
            throw new eBolnicaNotFoundException("Medication not found.");

        return await ctx.MedicationImages
            .AsNoTracking()
            .Where(i => i.MedicationId == request.MedicationId && !i.IsDeleted)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new MedicationImageDto
            {
                Id = i.Id,
                MedicationId = i.MedicationId,
                FileName = i.FileName,
                RelativeUrl = i.RelativeUrl,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder,
                FileSizeBytes = i.FileSizeBytes
            })
            .ToListAsync(ct);
    }
}
