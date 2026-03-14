using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eBolnicaAPI.Controllers
{
    [Route("api/patient/medical-record")]
    [ApiController]
    public class MedicalRecordController : ControllerBase
    {

        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        public MedicalRecordController(AppDbContext dbContext, UserManager<AppUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpGet("{id}/medical-records")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetMedicalRecordById(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == currentUserId);

            if (doctor == null)
                return Forbid();

            var patient = await _dbContext.Patients.Include(p => p.MedicalRecord).ThenInclude(mr=>mr.MedicalReports).Include(p=>p.AppUser).FirstOrDefaultAsync(p=>p.Id == id);

            if (patient == null)
            {
                return NotFound("Patient not found");
            }

            if (patient.MedicalRecord == null)
            {
                return NotFound("Medical record not found");
            }

            if (patient.DoctorId != doctor.Id)
                return Forbid();

            var records = new MedicalRecordDto
            {
                Id = patient.MedicalRecord.Id,
                PatientId = patient.MedicalRecord.PatientId,
                RecordNumber = patient.MedicalRecord.RecordNumber,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                IsAdmitted = patient.IsAdmitted,
                BloodType = patient.BloodType,
                Email = patient.AppUser.Email,

                Reports = patient.MedicalRecord.MedicalReports.OrderByDescending(r=>r.CreatedAt)
                .Select(r=>new MedicalReportCreateDto
                {
                    DoctorId = r.DoctorId,
                    CreatedAt = r.CreatedAt,
                    Diagnosis = r.Diagnosis,
                    Therapy = r.Therapy,
                    Symptoms = r.Symptoms
                }).ToList()
            };

            return Ok(records);
        }

        [HttpPost("new-medical-report")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> NewReport(MedicalReportCreateDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized("User not authenticated");
            }

            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d=>d.AppUserId == userId);

            if (doctor == null)
            {
                return NotFound("Doctor profile not found for this user");
            }

            var report = new MedicalReport
            {
                MedicalRecordId = dto.MedicalRecordId,
                CreatedAt = DateTime.Now,
                DoctorId = doctor.Id,
                Description = dto.Description,
                Diagnosis = dto.Diagnosis,
                Therapy = dto.Therapy,
                Symptoms = dto.Symptoms
            };

            _dbContext.MedicalReports.Add(report);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
