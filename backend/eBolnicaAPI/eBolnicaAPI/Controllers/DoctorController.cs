using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eBolnicaAPI.Controllers
{
    [Route("api/doctor")]
    [ApiController]
    public class DoctorController : ControllerBase
    {

        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        public DoctorController(AppDbContext dbContext, UserManager<AppUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpGet("doctor-data")]
        [Authorize(Roles ="Doctor")]
        public async Task<IActionResult> GetDoctorData()
        {
            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine("Doctor ID iz tokena: " + doctorId);

            if (doctorId == null)
            {
                return Unauthorized();
            }


            var doctorData = await _dbContext.Doctors
                .Include(d => d.AppUser)
                .FirstOrDefaultAsync(d => d.AppUserId == doctorId);


            if (doctorData == null)
            {
                return NotFound("Doctor not found");
            }

            return Ok(new
            {
                doctorData.FirstName,
                doctorData.LastName,
                doctorData.PhoneNumber,
                doctorData.Specialization,
                doctorData.BirthDate,
                doctorData.Address,
                doctorData.AppUser.Email
            });
            
        }
    }
}
