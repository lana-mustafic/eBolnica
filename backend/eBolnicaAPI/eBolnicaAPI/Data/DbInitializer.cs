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
            var patients = new[] { "p1", "p2", "p3", "p4", "p5", "p6", "p7", "p8", "p9", "p10", "p11", "p12" };

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
        }
    }
}
