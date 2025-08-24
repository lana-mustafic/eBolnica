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
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        public string LicenseNumber { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property: One Doctor can have many Appointments
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;

    }
}
