namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Data transfer object for prescription item information
    /// </summary>
    public class PrescriptionItemDto
    {
        public int Id { get; set; }

        public int PrescriptionId { get; set; }

        public int MedicationId { get; set; }

        public string MedicationName { get; set; }

        public int Quantity { get; set; }

        public string? Instructions { get; set; }

        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Calculated total price for this item
        /// </summary>
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
