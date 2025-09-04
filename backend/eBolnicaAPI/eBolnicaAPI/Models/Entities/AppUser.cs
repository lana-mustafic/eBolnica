using Microsoft.AspNetCore.Identity;

namespace eBolnicaAPI.Models.Entities
{
    public class AppUser:IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    
        public string UserType { get; set; }

        public string? LicenseNumber { get; set; }

        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }
    }
}
