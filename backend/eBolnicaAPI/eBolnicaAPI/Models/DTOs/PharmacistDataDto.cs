namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Data transfer object for pharmacist profile information
    /// </summary>
    public class PharmacistDataDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string LicenseNumber { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public DateTime HireDate { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }
    }
}
