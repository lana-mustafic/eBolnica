using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationById;

public sealed class GetMedicationByIdQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetMedicationByIdQuery, MedicationDto>
{
    public async Task<MedicationDto> Handle(GetMedicationByIdQuery request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .AsNoTracking()
            .Where(m => m.Id == request.Id && !m.IsDeleted)
            .Select(MedicationMapping.ToDetailDtoExpression)
            .FirstOrDefaultAsync(ct);

        if (medication is null)
            throw new eBolnicaNotFoundException("Medication not found.");

        return medication;
    }
}
