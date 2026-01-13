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
                MedicalRecordId = p.MedicalRecordId,
            }).ToList();    

            return Ok(dtoList);
        }

        [HttpPost("patients/{id}/medical-record/reports")]
        public async Task<IActionResult> AddReport(int id, [FromBody] MedicalReportCreateDto dto)
        {
            var record = await _dbContext.MedicalRecords.FirstOrDefaultAsync(r=>r.PatientId == id);

            if(record == null)
            {
                return NotFound("Record not found");
            }

            var report = new MedicalReport
            {
                MedicalRecordId = record.Id,
                DoctorId = dto.DoctorId,
                Diagnosis = dto.Diagnosis,
                Symptoms = dto.Symptoms,
                Therapy = dto.Therapy
            };

            _dbContext.MedicalReports.Add(report);
            await _dbContext.SaveChangesAsync();

            return Ok(report.Id);
        }

        [HttpGet("GetAllDoctors")]
        public async Task<IActionResult> GetDoctors()
        {

            var doctors = await _dbContext.Doctors.Select(d => new DoctorListDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
            }).ToListAsync();

            return Ok(doctors);
        }
    }
}
