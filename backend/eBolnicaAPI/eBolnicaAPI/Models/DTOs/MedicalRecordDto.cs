
namespace eBolnicaAPI.Models.DTOs
{
    public class MedicalRecordDto
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public string RecordNumber { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string BloodType { get; set; }

        public bool? IsAdmitted { get; set; }

        public string Email { get; set; }
        public List<MedicalReportCreateDto> Reports { get; internal set; }
    }
}
