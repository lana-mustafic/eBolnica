using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace eBolnicaAPI.Models.Entities
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public string RegistrationStatus { get; set; }
        public string? PhoneNumber { get; set; }

        public string? Specialization { get; set; }

        public string LicenseNumber { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property: One Doctor can have many Appointments
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;

        public DateTime? BirthDate { get; set; }
        public string? Address { get; set; }
    }
}
