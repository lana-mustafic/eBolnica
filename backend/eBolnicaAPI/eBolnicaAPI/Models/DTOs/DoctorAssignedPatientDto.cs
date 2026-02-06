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

        public string? RecordNumber { get; set; }

        public bool? IsAdmitted { get; set; }
    }

    public class CreatePatientDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters.")]
        public string? Gender { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        [StringLength(5, ErrorMessage = "Blood type cannot exceed 5 characters.")]
        public string? BloodType { get; set; }

        [StringLength(50, ErrorMessage = "Medical record ID cannot exceed 50 characters.")]
        public string? MedicalRecordId { get; set; }
    }

    public class UpdatePatientDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters.")]
        public string? Gender { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        [StringLength(5, ErrorMessage = "Blood type cannot exceed 5 characters.")]
        public string? BloodType { get; set; }

        [StringLength(50, ErrorMessage = "Medical record ID cannot exceed 50 characters.")]
        public string? MedicalRecordId { get; set; }
    }

    public class PatientSearchDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int? DoctorId { get; set; }
    }

    public class AssignPatientDto
    {
        [Required(ErrorMessage = "Patient ID is required.")]
        public int PatientId { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters.")]
        public string? Gender { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        [StringLength(5, ErrorMessage = "Blood type cannot exceed 5 characters.")]
        public string? BloodType { get; set; }

        [StringLength(50, ErrorMessage = "Medical record ID cannot exceed 50 characters.")]
        public string? MedicalRecordId { get; set; }
    }
}
