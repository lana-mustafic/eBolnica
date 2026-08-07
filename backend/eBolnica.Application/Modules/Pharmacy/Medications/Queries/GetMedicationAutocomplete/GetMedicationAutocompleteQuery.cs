namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationAutocomplete;

public sealed class GetMedicationAutocompleteQuery : IRequest<IReadOnlyList<MedicationAutocompleteSuggestionDto>>
{
    public string Query { get; init; } = string.Empty;
    public int Limit { get; init; } = 10;
    public bool? RequiresPrescription { get; init; }
}

public sealed class MedicationAutocompleteSuggestionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? Manufacturer { get; init; }
}
