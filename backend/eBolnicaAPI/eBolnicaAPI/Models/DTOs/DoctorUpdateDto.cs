using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.DTOs
{
    public class DoctorUpdateDto
    {
        [Required]
        [StringLength(30)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(30)]
        public string LastName { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; }

        [Required]
        [StringLength(100)]
        public string Specialization { get; set; }

    }
}
