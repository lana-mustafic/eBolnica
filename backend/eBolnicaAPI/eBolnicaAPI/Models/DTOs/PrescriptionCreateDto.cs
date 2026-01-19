using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Data transfer object for creating a prescription from a medical report
    /// </summary>
    public class PrescriptionCreateDto
    {
        [Required(ErrorMessage = "Medical report ID is required")]
        public int MedicalReportId { get; set; }

        [Required(ErrorMessage = "Patient ID is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Doctor ID is required")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "At least one prescription item is required")]
        [MinLength(1, ErrorMessage = "At least one prescription item is required")]
        public List<PrescriptionItemCreateDto> PrescriptionItems { get; set; } = new List<PrescriptionItemCreateDto>();

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Data transfer object for creating a prescription item
    /// </summary>
    public class PrescriptionItemCreateDto
    {
        [Required(ErrorMessage = "Medication ID is required")]
        public int MedicationId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
        public int Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Instructions cannot exceed 500 characters")]
        public string? Instructions { get; set; }
    }
}
