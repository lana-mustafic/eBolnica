using System.ComponentModel.DataAnnotations;
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

        public DateTime? BirthDate { get; set; }
        public string? Address { get; set; }
    }
}
