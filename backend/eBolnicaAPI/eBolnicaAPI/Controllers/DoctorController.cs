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

            var dto = new DoctorDataDto
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

        [HttpGet("list-patients")]
        [Authorize(Roles ="Doctor")]
        public async Task<IActionResult> GetDoctorAssignedPatient()
        {
            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if(doctor == null)
            {
                return NotFound();
            }

            var patients = await _dbContext.Patients.Where(p => p.DoctorId == doctor.Id).ToListAsync();

            if (!patients.Any())
                return NotFound("No patients assigned to this doctor.");

            var dtoList = patients.Select(p=> new DoctorAssignedPatientDto
            {
                Id = p.Id,
                DoctorId = (int)p.DoctorId,
                FirstName= p.FirstName,
                LastName= p.LastName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                PhoneNumber = p.PhoneNumber,
                Address = p.Address,
                BloodType = p.BloodType,
                MedicalRecordId = p.MedicalRecordId
            }).ToList();    

            return Ok(dtoList);
        }

        [HttpPost("create-patient")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto createPatientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(createPatientDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already exists." });
            }

            // Create AppUser
            var user = new AppUser
            {
                Email = createPatientDto.Email,
                UserName = createPatientDto.Email,
                FirstName = createPatientDto.FirstName,
                LastName = createPatientDto.LastName,
                UserType = "Patient"
            };

            var result = await _userManager.CreateAsync(user, createPatientDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Create Patient
            var patient = new Patient
            {
                AppUserId = user.Id,
                FirstName = createPatientDto.FirstName,
                LastName = createPatientDto.LastName,
                DoctorId = doctor.Id,
                DateOfBirth = createPatientDto.DateOfBirth,
                Gender = createPatientDto.Gender,
                PhoneNumber = createPatientDto.PhoneNumber,
                Address = createPatientDto.Address,
                BloodType = createPatientDto.BloodType,
                MedicalRecordId = createPatientDto.MedicalRecordId
            };

            _dbContext.Patients.Add(patient);
            await _dbContext.SaveChangesAsync();

            var responseDto = new DoctorAssignedPatientDto
            {
                Id = patient.Id,
                DoctorId = doctor.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                BloodType = patient.BloodType,
                MedicalRecordId = patient.MedicalRecordId
            };

            return Ok(responseDto);
        }

        [HttpPut("update-patient/{patientId}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdatePatient(int patientId, [FromBody] UpdatePatientDto updatePatientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == patientId && p.DoctorId == doctor.Id);

            if (patient == null)
            {
                return NotFound("Patient not found or not assigned to this doctor");
            }

            // Update patient fields
            patient.FirstName = updatePatientDto.FirstName;
            patient.LastName = updatePatientDto.LastName;
            patient.DateOfBirth = updatePatientDto.DateOfBirth;
            patient.Gender = updatePatientDto.Gender;
            patient.PhoneNumber = updatePatientDto.PhoneNumber;
            patient.Address = updatePatientDto.Address;
            patient.BloodType = updatePatientDto.BloodType;
            patient.MedicalRecordId = updatePatientDto.MedicalRecordId;

            // Update AppUser
            var appUser = await _userManager.FindByIdAsync(patient.AppUserId);
            if (appUser != null)
            {
                appUser.FirstName = updatePatientDto.FirstName;
                appUser.LastName = updatePatientDto.LastName;
                await _userManager.UpdateAsync(appUser);
            }

            await _dbContext.SaveChangesAsync();

            var responseDto = new DoctorAssignedPatientDto
            {
                Id = patient.Id,
                DoctorId = doctor.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                BloodType = patient.BloodType,
                MedicalRecordId = patient.MedicalRecordId
            };

            return Ok(responseDto);
        }

        [HttpDelete("delete-patient/{patientId}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DeletePatient(int patientId)
        {
            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            var patient = await _dbContext.Patients
                .Include(p => p.AppUser)
                .FirstOrDefaultAsync(p => p.Id == patientId && p.DoctorId == doctor.Id);

            if (patient == null)
            {
                return NotFound("Patient not found or not assigned to this doctor");
            }

            // First, remove the Patient entity
            _dbContext.Patients.Remove(patient);
            await _dbContext.SaveChangesAsync();

            // Then, delete the AppUser if it exists
            if (patient.AppUser != null)
            {
                var deleteResult = await _userManager.DeleteAsync(patient.AppUser);
                if (!deleteResult.Succeeded)
                {
                    // If user deletion fails, log the errors but patient is already deleted
                    return BadRequest(new { message = "Patient deleted but user deletion failed", errors = deleteResult.Errors });
                }
            }

            return Ok(new { message = "Patient deleted successfully" });
        }

        [HttpGet("search-patients")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> SearchPatients([FromQuery] string? searchTerm)
        {
            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            // Get all patients that are not assigned to this doctor
            var query = _dbContext.Patients
                .Include(p => p.AppUser)
                .Where(p => p.DoctorId == null || p.DoctorId != doctor.Id)
                .Where(p => p.AppUser != null); // Ensure AppUser exists

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(p =>
                    (p.FirstName != null && p.FirstName.ToLower().Contains(searchTerm)) ||
                    (p.LastName != null && p.LastName.ToLower().Contains(searchTerm)) ||
                    (p.AppUser != null && p.AppUser.Email != null && p.AppUser.Email.ToLower().Contains(searchTerm)));
            }

            var patients = await query
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.LastName)
                .Select(p => new PatientSearchDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName ?? "",
                    LastName = p.LastName ?? "",
                    Email = p.AppUser != null && p.AppUser.Email != null ? p.AppUser.Email : "",
                    DoctorId = p.DoctorId
                })
                .Take(20)
                .ToListAsync();

            return Ok(patients);
        }

        [HttpPost("assign-patient")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> AssignPatient([FromBody] AssignPatientDto assignPatientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            var patient = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == assignPatientDto.PatientId);

            if (patient == null)
            {
                return NotFound("Patient not found");
            }

            // Check if patient is already assigned to another doctor
            if (patient.DoctorId != null && patient.DoctorId != doctor.Id)
            {
                return BadRequest(new { message = "Patient is already assigned to another doctor." });
            }

            // Assign patient to doctor and update details
            patient.DoctorId = doctor.Id;
            patient.DateOfBirth = assignPatientDto.DateOfBirth ?? patient.DateOfBirth;
            patient.Gender = assignPatientDto.Gender ?? patient.Gender;
            patient.PhoneNumber = assignPatientDto.PhoneNumber ?? patient.PhoneNumber;
            patient.Address = assignPatientDto.Address ?? patient.Address;
            patient.BloodType = assignPatientDto.BloodType ?? patient.BloodType;
            patient.MedicalRecordId = assignPatientDto.MedicalRecordId ?? patient.MedicalRecordId;

            await _dbContext.SaveChangesAsync();

            var responseDto = new DoctorAssignedPatientDto
            {
                Id = patient.Id,
                DoctorId = doctor.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                BloodType = patient.BloodType,
                MedicalRecordId = patient.MedicalRecordId
            };

            return Ok(responseDto);
        }
    }
}
