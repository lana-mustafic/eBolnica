using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Controllers
{
    [Route("api/accounts")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _dbcontext;
        private readonly IJwtService _jwtService;

        public AccountsController(AppDbContext dbcontext,UserManager<AppUser> userManager, IJwtService jwtService)
        {
            _dbcontext = dbcontext;
            _userManager = userManager;
            _jwtService = jwtService;
        }

        [HttpPost("patient-registration")]
        public async Task<IActionResult> RegisterPatient([FromBody] PatientRegistrationDto patientForRegistration)
        {
            if (patientForRegistration == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            try
            {
                var user = new AppUser
                {
                    Email = patientForRegistration.Email,
                    UserName = patientForRegistration.Email,
                    FirstName = patientForRegistration.FirstName,
                    LastName = patientForRegistration.LastName,
                    UserType = "Patient",
                };

                var result = await _userManager.CreateAsync(user, patientForRegistration.Password);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(result.Errors);
                }

                user = await _userManager.FindByEmailAsync(user.Email);
                if (user == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "Failed to retrieve created user." });
                }

                var patient = new Patient
                {
                    AppUserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DoctorId = null,
                    RegistrationStatus = "Pending",
                    DateOfBirth = patientForRegistration.DateOfBirth,
                    Gender = patientForRegistration.Gender
                };

                _dbcontext.Patients.Add(patient);
                await _dbcontext.SaveChangesAsync();

                var record = new MedicalRecord
                {
                    PatientId = patient.Id,
                    RecordNumber = $"MR-{DateTime.UtcNow:yyyy}-{patient.Id}",
                };

                _dbcontext.MedicalRecords.Add(record);
                await _dbcontext.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { message = "Patient registered successfully" });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred while registering the patient." });
            }
        }

        [HttpPost("doctor-registration")]
        public async Task<IActionResult> RegisterDoctor([FromBody] DoctorRegistrationDto doctorForRegistration)
        {
            if (doctorForRegistration == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            bool licenseExist = await _dbcontext.Doctors.AnyAsync(d => d.LicenseNumber == doctorForRegistration.LicenseNumber);

            if (licenseExist)
            {
                var error = new IdentityError
                {
                    Code = "LicenseNumberExists",
                    Description = "License Number is already in use."
                };

                return BadRequest(error);
            }

            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            try
            {
                var user = new AppUser
                {
                    Email = doctorForRegistration.Email,
                    UserName = doctorForRegistration.Email,
                    FirstName = doctorForRegistration.FirstName,
                    LastName = doctorForRegistration.LastName,
                    LicenseNumber = doctorForRegistration.LicenseNumber,
                    UserType = "Doctor"
                };

                var result = await _userManager.CreateAsync(user, doctorForRegistration.Password);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(result.Errors);
                }

                var doctor = new Doctor
                {
                    AppUserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    LicenseNumber = user.LicenseNumber,
                    RegistrationStatus = "Pending",
                    BirthDate = doctorForRegistration.DateOfBirth,
                    Gender = doctorForRegistration.Gender
                };

                _dbcontext.Doctors.Add(doctor);
                await _dbcontext.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { message = "Doctor registered successfully" });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred while registering the doctor." });
            }
        }


        [HttpPost("user-login")]
        public async Task<IActionResult> UserLogin([FromBody] UserLoginDto login)
        {
            var user = await _userManager.FindByEmailAsync(login.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, login.Password))
            {
                return Unauthorized("Invalid login attempt");
            }

            if (user.UserType == "Doctor")
            {
                var doctor = await _dbcontext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == user.Id);

                if (doctor == null || !string.Equals(doctor.RegistrationStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Your account is not approved.");
                }
            }

            if (user.UserType == "Patient")
            {
                var patient = await _dbcontext.Patients.FirstOrDefaultAsync(p => p.AppUserId == user.Id);

                if (patient == null || !string.Equals(patient.RegistrationStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Your account is not approved.");
                }
            }

            var token = _jwtService.GenerateToken(user);

            return Ok(new {Token = token});
        }
    
    }
}
