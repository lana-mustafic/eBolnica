namespace eBolnicaAPI.Models.Entities
{
    public class MedicalRecord
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public Patient Patient { get; set; }

        public string RecordNumber { get; set; }

        public ICollection<MedicalReport> Reports { get; set; }
    }
}
