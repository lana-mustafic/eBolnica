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
            int pageSize = 10,
            string? userType = null,
            string sortBy = "firstName",
            string sortDirection = "asc"
        )
        {
            pageSize = Math.Clamp(pageSize, 1, 100);

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var query = _dbContext.AppUsers
                .Where(u => u.Id != currentUserId)
                .Include(u => u.Doctor)
                .Include(u => u.Patient)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userType))
            {
                query = query.Where(u => u.UserType == userType);
            }

            query = ApplySorting(query, sortBy.ToLower(), sortDirection.ToLower());

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserOverviewDto
                {
                    AppUserId = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    UserType = u.UserType,
                    RegistrationStatus = u.UserType == "Doctor"
                        ? u.Doctor != null ? u.Doctor.RegistrationStatus : null
                        : u.UserType == "Patient"
                            ? u.Patient != null ? u.Patient.RegistrationStatus : null
                            : null,
                    LicenseNumber = u.UserType == "Doctor" ? u.Doctor != null ? u.Doctor.LicenseNumber : null : null
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Users = users,
                SortBy = sortBy,
                SortDirection = sortDirection,
            });
        }

        [HttpPut("update-registration-status/{AppUserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRegistrationStatus(string AppUserId, [FromBody] UpdateRegistrationStatusDto dto)
        {
            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == AppUserId);

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            doctor.RegistrationStatus = dto.RegistrationStatus;

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Doctor registration status updated successfully." });
        }

        private IQueryable<AppUser> ApplySorting(IQueryable<AppUser> query, string sortBy, string sortDirection)
        {
            return sortBy switch
            {
                "firstname" => sortDirection == "asc" ? query.OrderBy(u => u.FirstName) : query.OrderByDescending(u => u.FirstName),
                "lastname" => sortDirection == "asc" ? query.OrderBy(u => u.LastName) : query.OrderByDescending(u => u.LastName),
                "email" => sortDirection == "asc" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                "usertype" => sortDirection == "asc" ? query.OrderBy(u => u.UserType) : query.OrderByDescending(u => u.UserType),
                _ => sortDirection == "asc" ? query.OrderBy(u => u.FirstName) : query.OrderByDescending(u => u.FirstName)
            };
        }

        [HttpPost("create-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "User with this email already exists." });

            Doctor? assignedDoctor = null;

            if (dto.UserType == "Patient")
            {
                if (!dto.DoctorId.HasValue)
                    return BadRequest(new { message = "DoctorId is required for patient creation." });

                assignedDoctor = await _dbContext.Doctors
                    .FirstOrDefaultAsync(d => d.Id == dto.DoctorId.Value);

                if (assignedDoctor == null)
                    return BadRequest(new { message = "Selected doctor not found." });
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var user = new AppUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    UserType = dto.UserType
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                switch (dto.UserType)
                {
                    case "Doctor":
                        _dbContext.Doctors.Add(new Doctor
                        {
                            AppUserId = user.Id,
                            FirstName = dto.FirstName,
                            LastName = dto.LastName,
                            RegistrationStatus = "Pending"
                        });
                        break;

                    case "Patient":
                        _dbContext.Patients.Add(new Patient
                        {
                            AppUserId = user.Id,
                            FirstName = dto.FirstName,
                            LastName = dto.LastName,
                            DoctorId = assignedDoctor!.Id,
                            RegistrationStatus = "Approved",
                            MedicalRecord = new MedicalRecord
                            {
                                RecordNumber = $"MR-{DateTime.UtcNow:yyyyMMdd}-{user.Id[..4]}"
                            }
                        });
                        break;

                    case "Pharmacist":
                        _dbContext.Pharmacists.Add(new Pharmacist
                        {
                            AppUserId = user.Id,
                            FirstName = dto.FirstName,
                            LastName = dto.LastName
                        });
                        break;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "User created successfully." });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred while creating the user." });
            }
        }

        [HttpPut("update-user/{appUserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(string appUserId, [FromBody] UpdateUserDto dto)
        {
            var user = await _dbContext.AppUsers
                .Include(u => u.Doctor)
                .Include(u => u.Patient)
                .Include(u => u.Pharmacist)
                .FirstOrDefaultAsync(u => u.Id == appUserId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.UserName = dto.Email;

            if (user.Doctor != null)
            {
                user.Doctor.FirstName = dto.FirstName;
                user.Doctor.LastName = dto.LastName;
            }

            if (user.Patient != null)
            {
                user.Patient.FirstName = dto.FirstName;
                user.Patient.LastName = dto.LastName;
            }

            if (user.Pharmacist != null)
            {
                user.Pharmacist.FirstName = dto.FirstName;
                user.Pharmacist.LastName = dto.LastName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "User updated successfully." });
        }


        [HttpDelete("delete-user/{appUserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string appUserId)
        {
            var user = await _dbContext.AppUsers
                .Include(u => u.Doctor)
                .Include(u => u.Patient)
                    .ThenInclude(p => p.MedicalRecord)
                .Include(u => u.Pharmacist)
                .FirstOrDefaultAsync(u => u.Id == appUserId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (user.Doctor != null)
                {
                    _dbContext.Doctors.Remove(user.Doctor);
                }

                if (user.Patient != null)
                {
                    if (user.Patient.MedicalRecord != null)
                    {
                        _dbContext.MedicalRecords.Remove(user.Patient.MedicalRecord);
                    }

                    _dbContext.Patients.Remove(user.Patient);
                }

                if (user.Pharmacist != null)
                {
                    _dbContext.Pharmacists.Remove(user.Pharmacist);
                }

                await _dbContext.SaveChangesAsync();

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                await transaction.CommitAsync();
                return Ok(new { message = "User deleted successfully." });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred while deleting the user." });
            }
        }

        [HttpPut("update-patient-registration-status/{appUserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePatientRegistrationStatus(string appUserId, [FromBody] UpdateRegistrationStatusDto dto)
        {
            var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.AppUserId == appUserId);

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            patient.RegistrationStatus = dto.RegistrationStatus;

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Patient registration status updated successfully." });
        }


    }
}
