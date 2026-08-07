namespace eBolnica.Application.Modules.Pharmacy.Medications;

public sealed class MedicationStockHistoryDto
{
    public int Id { get; init; }
    public DateTime OccurredAt { get; init; }
    public int ChangeQuantity { get; init; }
    public int StockAfter { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ReferenceLabel { get; init; }
}
