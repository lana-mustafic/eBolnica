namespace eBolnica.Application.Modules.Pharmacy.Medications;

public sealed class MedicationDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? GenericName { get; init; }
    public string? Description { get; init; }
    public string? Manufacturer { get; init; }
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public int MinimumStockLevel { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? BatchNumber { get; init; }
    public bool IsActive { get; init; }
    public bool RequiresPrescription { get; init; }
    public string? Category { get; init; }
    public string? DosageForm { get; init; }
    public string? Strength { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? PrimaryImageUrl { get; init; }
}
