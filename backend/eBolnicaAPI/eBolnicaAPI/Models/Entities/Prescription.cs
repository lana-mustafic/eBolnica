using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.Entities
{
    /// <summary>
    /// Represents a prescription linked to a medical report
    /// </summary>
    public class Prescription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PrescriptionNumber { get; set; }

        [Required]
        public int MedicalReportId { get; set; }

        public MedicalReport MedicalReport { get; set; }

        [Required]
        public int PatientId { get; set; }

        public Patient Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }

        public Doctor Doctor { get; set; }

        public int? PharmacistId { get; set; }

        public Pharmacist? Pharmacist { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public DateTime PrescribedDate { get; set; }

        public DateTime? DispensedDate { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    }
}
