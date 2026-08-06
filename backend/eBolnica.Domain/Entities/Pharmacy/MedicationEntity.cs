using eBolnica.Domain.Common;

namespace eBolnica.Domain.Entities.Pharmacy;

public sealed class MedicationEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int MinimumStockLevel { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresPrescription { get; set; }
    public string? Category { get; set; }
    public string? DosageForm { get; set; }
    public string? Strength { get; set; }
    public string? ImageUrl { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<MedicationImageEntity> Images { get; set; } = new List<MedicationImageEntity>();

    public static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
}
