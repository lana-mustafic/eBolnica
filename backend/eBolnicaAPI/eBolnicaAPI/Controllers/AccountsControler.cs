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


            var user = new AppUser
            {
                Email = patientForRegistration.Email,
                UserName = patientForRegistration.Email,
                FirstName = patientForRegistration.FirstName,
                LastName = patientForRegistration.LastName,
                UserType = "Patient"
            };

            var result = await _userManager.CreateAsync(user, patientForRegistration.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var patient = new Patient
            {
                AppUserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            _dbcontext.Patients.Add(patient);
            await _dbcontext.SaveChangesAsync();

            return Ok(new { message = "Patient registered successfully" });

        }

        [HttpPost("doctor-registration")]
        public async Task<IActionResult> RegisterDoctor([FromBody] DoctorRegistrationDto doctorForRegistration)
        {
            if (doctorForRegistration == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            bool licenseExist =await _dbcontext.Doctors.AnyAsync(d=>d.LicenseNumber==doctorForRegistration.LicenseNumber);

            if (licenseExist)
            {
                var error = new IdentityError
                {
                    Code = "LicenseNumberExists",
                    Description="License Number is already in use."
                };

                return BadRequest(error);
            }
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
                return BadRequest(result.Errors);
            }

            var doctor = new Doctor
            {
                AppUserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                LicenseNumber= user.LicenseNumber,
                RegistrationStatus="Pending"
            };

            
            _dbcontext.Doctors.Add(doctor);
            await _dbcontext.SaveChangesAsync();

            return Ok(new { message = "Doctor registered successfully" });

        }

        [HttpPost("user-login")]
        public async Task<IActionResult> UserLogin([FromBody] UserLoginDto login)
        {
            var user = await _userManager.FindByEmailAsync(login.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, login.Password))
            {
                return Unauthorized("Invalid login attempt");
            }
            if(user.UserType == "Doctor")
            {
                var doctor = await _dbcontext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == user.Id);

                if (doctor == null || !string.Equals(doctor.RegistrationStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Your account is not approved.");
                }
            }
            var token = _jwtService.GenerateToken(user);

            return Ok(new {Token = token});
        }

    }
}
