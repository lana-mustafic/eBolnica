using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.Entities
{
    /// <summary>
    /// Represents a medication in the pharmacy inventory
    /// </summary>
    public class Medication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? GenericName { get; set; }

        public string? Description { get; set; }

        public string? Manufacturer { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        [Required]
        public int MinimumStockLevel { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string? BatchNumber { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public bool RequiresPrescription { get; set; }

        public string? Category { get; set; }

        public string? DosageForm { get; set; }

        public string? Strength { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();

        public ICollection<MedicationImage> Images { get; set; } = new List<MedicationImage>();
    }
}
