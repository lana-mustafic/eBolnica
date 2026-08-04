using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eBolnicaAPI.Controllers
{
    [Route("api/patient")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;

        public PatientController(AppDbContext dbContext, UserManager<AppUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpGet("patient-data")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetPatientData()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            var patientData = await _dbContext.Patients
                .Include(p => p.AppUser)
                .FirstOrDefaultAsync(p => p.AppUserId == userId);

            if (patientData == null)
            {
                return NotFound("Patient not found");
            }

            var dto = new PatientDataDto
            {
                Id = patientData.Id,
                FirstName = patientData.FirstName,
                LastName = patientData.LastName
            };

            return Ok(dto);
        }
    }
}

