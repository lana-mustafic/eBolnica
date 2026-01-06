namespace eBolnicaAPI.Models.DTOs
{
    public class MedicalReportCreateDto
    {
        public int DoctorId { get; set; }
        public string Diagnosis { get; set; }
        public string Symptoms { get; set; }
        public string Therapy { get; set; }
    }
}
