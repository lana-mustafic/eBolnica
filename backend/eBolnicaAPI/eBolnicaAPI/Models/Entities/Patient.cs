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
        public string UserId { get; set; }
        public AppUser User { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string BloodType { get; set; }

        public string MedicalRecordId { get; set; }

        public string Allergies { get; set; }

        public bool IsAdmitted { get; set; }
    }
}
