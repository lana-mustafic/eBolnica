using eBolnicaAPI.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.DTOs
{
    public class DoctorAssignedPatientDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int DoctorId { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? BloodType { get; set; }

        public string? MedicalRecordId { get; set; }

        public bool? IsAdmitted { get; set; }
    }
}
