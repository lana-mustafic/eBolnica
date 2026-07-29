using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.Entities
{
    /// <summary>
    /// Point-in-time stock level for a medication, used for inventory trend analytics.
    /// </summary>
    public class MedicationStockHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MedicationId { get; set; }

        public Medication Medication { get; set; } = null!;

        /// <summary>Stock quantity at the time of recording.</summary>
        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Origin of the snapshot (e.g. Dispense, ManualUpdate, Initial).</summary>
        [Required]
        [MaxLength(64)]
        public string Source { get; set; } = string.Empty;
    }
}
