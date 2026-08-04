using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.Entities
{
    /// <summary>
    /// Represents a line item in a prescription
    /// </summary>
    public class PrescriptionItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PrescriptionId { get; set; }

        public Prescription Prescription { get; set; }

        [Required]
        public int MedicationId { get; set; }

        public Medication Medication { get; set; }

        [Required]
        public int Quantity { get; set; }

        public string? Instructions { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
