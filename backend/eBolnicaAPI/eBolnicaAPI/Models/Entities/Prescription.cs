using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eBolnicaAPI.Models.Entities
{
    public class Prescription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        [StringLength(100)]
        public string Medication { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Dosage { get; set; } = string.Empty;

        public string Instructions { get; set; } = string.Empty;

        [Required]
        public DateTime DatePrescribed { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpirationDate { get; set; }

        public int RefillsRemaining { get; set; } = 0;

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;
    }
}