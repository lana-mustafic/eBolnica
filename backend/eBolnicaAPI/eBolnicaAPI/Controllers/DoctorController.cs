using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
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
            Console.WriteLine("Doctor ID from token: " + doctorId);

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

            var dto = new DoctorDataDbo
            {
                FirstName = doctorData.FirstName,
                LastName = doctorData.LastName,
                PhoneNumber = doctorData.PhoneNumber,
                Specialization = doctorData.Specialization,
                LicenseNumber = doctorData.AppUser.LicenseNumber,
                BirthDate = (DateTime)doctorData.BirthDate,
                Address = doctorData.Address,
                Email = doctorData.AppUser.Email
            };

            return Ok(dto);
            
        }

        [HttpPut("edit-doctor")]
        [Authorize(Roles="Doctor")]
        public async Task<IActionResult> EditDoctor([FromBody] DoctorUpdateDto UpdatedDoctorDto)
        {
            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }


            var existingDoctor = await _dbContext.Doctors.Include(d => d.AppUser).FirstOrDefaultAsync(d=>d.AppUserId==doctorId);

            if (existingDoctor == null)
            {
                return NotFound();
            }

            existingDoctor.FirstName= UpdatedDoctorDto.FirstName;
            existingDoctor.LastName= UpdatedDoctorDto.LastName;
            existingDoctor.BirthDate= UpdatedDoctorDto.BirthDate;
            existingDoctor.PhoneNumber= UpdatedDoctorDto.PhoneNumber;
            existingDoctor.Address= UpdatedDoctorDto.Address;
            existingDoctor.Specialization= UpdatedDoctorDto.Specialization;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);

            }

            await _dbContext.SaveChangesAsync();


            return Ok(UpdatedDoctorDto);

        }
    }
}
