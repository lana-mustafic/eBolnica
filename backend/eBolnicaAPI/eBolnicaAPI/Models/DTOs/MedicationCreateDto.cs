using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Data transfer object for creating or updating medication
    /// </summary>
    public class MedicationCreateDto
    {
        [Required(ErrorMessage = "Medication name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Generic name cannot exceed 100 characters")]
        public string? GenericName { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Manufacturer cannot exceed 100 characters")]
        public string? Manufacturer { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between 0.01 and 10,000")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 10000, ErrorMessage = "Stock quantity must be between 0 and 10,000")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Minimum stock level is required")]
        [Range(0, 10000, ErrorMessage = "Minimum stock level must be between 0 and 10,000")]
        public int MinimumStockLevel { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters")]
        public string? BatchNumber { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "RequiresPrescription is required")]
        public bool RequiresPrescription { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; }

        [StringLength(50, ErrorMessage = "Dosage form cannot exceed 50 characters")]
        public string? DosageForm { get; set; }

        [StringLength(50, ErrorMessage = "Strength cannot exceed 50 characters")]
        public string? Strength { get; set; }
    }
}
