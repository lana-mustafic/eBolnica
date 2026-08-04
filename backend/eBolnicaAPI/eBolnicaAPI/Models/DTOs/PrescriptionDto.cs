namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Data transfer object for complete prescription information
    /// </summary>
    public class PrescriptionDto
    {
        public int Id { get; set; }

        public string PrescriptionNumber { get; set; }

        public int MedicalReportId { get; set; }

        public int PatientId { get; set; }

        public PatientDataDto Patient { get; set; }

        public int DoctorId { get; set; }

        public DoctorDataDto Doctor { get; set; }

        public int? PharmacistId { get; set; }

        public PharmacistDataDto? Pharmacist { get; set; }

        public string Status { get; set; }

        public DateTime PrescribedDate { get; set; }

        public DateTime? DispensedDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<PrescriptionItemDto> PrescriptionItems { get; set; } = new List<PrescriptionItemDto>();
    }
}
