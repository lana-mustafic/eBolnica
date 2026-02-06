namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Data transfer object for medication information
    /// </summary>
    public class MedicationDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string? GenericName { get; set; }

        public string? Description { get; set; }

        public string? Manufacturer { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public int MinimumStockLevel { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string? BatchNumber { get; set; }

        public bool IsActive { get; set; }

        public bool RequiresPrescription { get; set; }

        public string? Category { get; set; }

        public string? DosageForm { get; set; }

        public string? Strength { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Indicates if medication stock is below minimum level
        /// </summary>
        public bool IsLowStock => StockQuantity < MinimumStockLevel;

        /// <summary>
        /// Indicates if medication has expired
        /// </summary>
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;
    }
}
