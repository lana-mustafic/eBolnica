namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.CheckMedicationName;

public sealed class CheckMedicationNameQuery : IRequest<CheckMedicationNameQueryDto>
{
    public string Name { get; init; } = string.Empty;
    public int? ExcludeId { get; init; }
}

public sealed class CheckMedicationNameQueryDto
{
    public bool IsAvailable { get; init; }
}
