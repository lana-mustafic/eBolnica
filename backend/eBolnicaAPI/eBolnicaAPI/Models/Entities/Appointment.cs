using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eBolnicaAPI.Models.Entities
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        public string Status { get; set; } = "Scheduled";

        public string? Reason { get; set; }

        // Foreign Keys
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        // Navigation Properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;
    }
}