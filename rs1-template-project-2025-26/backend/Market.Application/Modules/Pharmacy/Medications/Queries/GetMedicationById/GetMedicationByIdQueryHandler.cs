using Market.Application.Modules.Pharmacy.Medications;
using Market.Application.Modules.Pharmacy.Medications.Queries.GetMedicationById;
using Market.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

public sealed class GetMedicationByIdQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetMedicationByIdQuery, MedicationDto>
{
    public async Task<MedicationDto> Handle(GetMedicationByIdQuery request, CancellationToken ct)
    {
        var medication = await ctx.Medications
            .AsNoTracking()
            .Where(m => m.Id == request.Id && !m.IsDeleted)
            .Select(MedicationMapping.ToDtoExpression)
            .FirstOrDefaultAsync(ct);

        if (medication is null)
            throw new MarketNotFoundException("Medication not found.");

        return medication;
    }
}
