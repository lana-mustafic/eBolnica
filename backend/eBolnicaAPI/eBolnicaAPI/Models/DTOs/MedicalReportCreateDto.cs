namespace eBolnicaAPI.Models.DTOs
{
    public class MedicalReportCreateDto
    {
        public int MedicalRecordId { get; set; }
        public DateTime CreatedAt { get; set; }

        public int DoctorId { get; set; }
        public string Description { get; set; } = string.Empty;

        public string? Diagnosis { get; set; }
        public string? Therapy { get; set; }
       
        public string? Symptoms { get; set; }
    }
}
