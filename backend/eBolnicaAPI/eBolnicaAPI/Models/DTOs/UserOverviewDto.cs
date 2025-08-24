namespace eBolnicaAPI.Models.DTOs
{
    public class UserOverviewDto
    {
        public string AppUserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string UserType { get; set; }
        public string? RegistrationStatus { get; set; }
        public string? LicenseNumber { get; set; }
    }
}
