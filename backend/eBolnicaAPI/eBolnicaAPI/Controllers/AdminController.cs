using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace eBolnicaAPI.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(AppDbContext dbContext, UserManager<AppUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpGet("list-users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _dbContext.AppUsers.Include(u=>u.Doctor).Include(u=>u.Patient).Select(u => new
            {
                u.FirstName,
                u.LastName,
                u.Email,
                u.UserType,

                DoctorInfo = u.UserType=="Doctor" ? new
                {
                    u.Doctor.LicenseNumber,
                    u.Doctor.RegistrationStatus
                } : null

            }).ToListAsync();

            return Ok(users);
        }

        [HttpPut("update-registration-status/{AppUserId}")]
        public async Task<IActionResult> UpdateRegistrationStatus(string AppUserId, [FromBody]UpdateRegistrationStatusDto dto)
        {
            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d=>d.AppUserId == AppUserId);

            if (doctor == null)
            {
                return NotFound(new {message="Doctor not found."});
            }

            doctor.RegistrationStatus = dto.RegistrationStatus;

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Doctor registration status updated successfully." });
        }
        
    }
}
