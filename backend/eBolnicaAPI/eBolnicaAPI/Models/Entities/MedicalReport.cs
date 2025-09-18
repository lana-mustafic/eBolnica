namespace eBolnicaAPI.Models.Entities
{
    public class MedicalReport
    {
        public int Id { get; set; }

        public int MedicalRecordId { get; set; }

        public MedicalRecord MedicalRecord { get; set; }

        public int DoctorId { get; set; }

        public Doctor Doctor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Diagnosis { get; set; }

        public string Symptoms { get; set; }

        public string Therapy { get; set; }
    }
}
