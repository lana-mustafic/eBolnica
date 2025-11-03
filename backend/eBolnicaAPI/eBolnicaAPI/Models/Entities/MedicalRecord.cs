using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eBolnicaAPI.Models.Entities
{
    public class MedicalRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; } // Foreign key for Patient

        public string Diagnosis { get; set; } = string.Empty;

        public string Treatment { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

        // Navigation property to Patient
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;
    }
}