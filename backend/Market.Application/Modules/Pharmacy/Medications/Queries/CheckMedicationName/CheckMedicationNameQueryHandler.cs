using Market.Application.Modules.Pharmacy.Medications.Queries.CheckMedicationName;
using Market.Domain.Entities.Pharmacy;

public sealed class CheckMedicationNameQueryHandler(IAppDbContext ctx)
    : IRequestHandler<CheckMedicationNameQuery, CheckMedicationNameQueryDto>
{
    public async Task<CheckMedicationNameQueryDto> Handle(CheckMedicationNameQuery request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new MarketBusinessRuleException("validation.failed", "Medication name is required.");

        var normalized = MedicationEntity.NormalizeName(name);
        var taken = await ctx.Medications.AnyAsync(
            m => !m.IsDeleted && m.NormalizedName == normalized && m.Id != request.ExcludeId,
            ct);

        return new CheckMedicationNameQueryDto { IsAvailable = !taken };
    }
}
