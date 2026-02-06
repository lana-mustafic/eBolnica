using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.Entities
{
    public class Patient
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

        public int? DoctorId { get; set; }

        public Doctor Doctor { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? BloodType { get; set; }

        public MedicalRecord MedicalRecord { get; set; }

        public bool? IsAdmitted { get; set; }

        public virtual ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
    }
}
