using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.Entities
{
    /// <summary>
    /// Represents a pharmacist in the pharmacy module
    /// </summary>
    public class Pharmacist
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

        [Required]
        public string LicenseNumber { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public DateTime HireDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Prescription> DispensedPrescriptions { get; set; } = new List<Prescription>();
    }
}
