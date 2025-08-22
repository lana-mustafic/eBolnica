using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.Entities
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string BloodType { get; set; } = string.Empty;

        public string MedicalRecordId { get; set; } = string.Empty;

        public string Allergies { get; set; } = string.Empty;

        public bool IsAdmitted { get; set; } = false;

        // Navigation property: One Patient can have many Appointments
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
