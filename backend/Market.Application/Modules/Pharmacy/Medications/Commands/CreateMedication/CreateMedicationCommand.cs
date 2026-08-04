namespace Market.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;

public sealed class CreateMedicationCommand : IRequest<MedicationDto>
{
    public string Name { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? Description { get; init; }
    public string? Manufacturer { get; init; }
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public int MinimumStockLevel { get; init; }
    public DateTime ExpiryDate { get; init; }
    public string? BatchNumber { get; init; }
    public bool IsActive { get; init; } = true;
    public bool RequiresPrescription { get; init; }
    public string Category { get; init; } = string.Empty;
    public string? DosageForm { get; init; }
    public string? Strength { get; init; }
}
