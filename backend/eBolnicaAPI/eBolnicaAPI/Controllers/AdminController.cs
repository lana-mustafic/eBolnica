using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Validations;

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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers(
                int page = 1,
                int pageSize=10,
                string? userType = null
            )
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var query = _dbContext.AppUsers.Where(u => u.Id != currentUserId).Include(u => u.Doctor).Include(u => u.Patient).AsQueryable();

            if (!string.IsNullOrEmpty(userType))
            {
                query=query.Where(u=>u.UserType == userType);
            }

            var totalCount = await query.CountAsync();

            var users = await query.OrderBy(u=>u.FirstName).Skip((page-1)*pageSize).Take(pageSize).Select(u => new
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

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Users = users
            });
        }

        [HttpPut("update-registration-status/{AppUserId}")]
        [Authorize(Roles="Admin")]
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
