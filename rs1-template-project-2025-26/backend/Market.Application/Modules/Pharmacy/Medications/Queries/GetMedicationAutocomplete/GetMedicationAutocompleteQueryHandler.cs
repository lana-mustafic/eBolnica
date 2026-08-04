using Market.Application.Modules.Pharmacy.Medications.Queries.GetMedicationAutocomplete;
using Market.Domain.Entities.Pharmacy;

public sealed class GetMedicationAutocompleteQueryHandler(IAppDbContext ctx)
    : IRequestHandler<GetMedicationAutocompleteQuery, IReadOnlyList<MedicationAutocompleteSuggestionDto>>
{
    public async Task<IReadOnlyList<MedicationAutocompleteSuggestionDto>> Handle(
        GetMedicationAutocompleteQuery request, CancellationToken ct)
    {
        var trimmed = request.Query?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
            return Array.Empty<MedicationAutocompleteSuggestionDto>();

        var normalized = MedicationEntity.NormalizeName(trimmed);
        var limit = Math.Clamp(request.Limit, 1, 10);

        return await ctx.Medications
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.IsActive)
            .Where(m =>
                m.NormalizedName.Contains(normalized) ||
                (m.GenericName != null && m.GenericName.ToLower().Contains(normalized)) ||
                (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(normalized)))
            .OrderBy(m => m.Name)
            .Take(limit)
            .Select(m => new MedicationAutocompleteSuggestionDto
            {
                Id = m.Id,
                Name = m.Name,
                Category = m.Category,
                Manufacturer = m.Manufacturer
            })
            .ToListAsync(ct);
    }
}
