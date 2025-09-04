using eBolnicaAPI.Models;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace eBolnicaAPI.Data
{
    public static class DbInitializer
    {
        public static async Task SeedPasswords(UserManager<AppUser> userManager)
        {
            string doctorPassword = "Doctor123!";
            string patientPassword = "Patient123!";


            var doctors = new[] { "d1", "d2", "d3" };
            var patients = new[] { "p1", "p2", "p3" };

            foreach (var id in doctors)
            {
                var user = await userManager.FindByIdAsync(id);
                if (user != null)
                {
                    await userManager.AddPasswordAsync(user, doctorPassword);
                }
            }

            foreach (var id in patients)
            {
                var user = await userManager.FindByIdAsync(id);
                if (user != null)
                {
                    await userManager.AddPasswordAsync(user, patientPassword);
                }
            }

            var admin = await userManager.FindByNameAsync("admin@gmail.com");
            if (admin != null && string.IsNullOrEmpty(admin.PasswordHash))
            {
                await userManager.AddPasswordAsync(admin, "Admin123!");
            }
        }
    }
}
