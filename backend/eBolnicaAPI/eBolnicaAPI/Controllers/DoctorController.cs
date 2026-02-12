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
                PhoneNumber = doctorData.PhoneNumber ?? "N/A",
                Specialization = doctorData.Specialization,
                LicenseNumber = doctorData.AppUser.LicenseNumber,
                BirthDate = doctorData.BirthDate,
                Address = doctorData.Address ?? "N/A",
                Email = doctorData.AppUser?.Email ?? "N/A"
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

            var patients = await _dbContext.Patients.Include(p=>p.MedicalRecord).Where(p => p.DoctorId == doctor.Id).ToListAsync();

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
                RecordNumber = p.MedicalRecord.RecordNumber,
            }).ToList();    

            return Ok(dtoList);
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

        [HttpGet("doctor-stats")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> ShowDoctorStats()
        {
            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if (doctor == null)
            {
                return NotFound();
            }

            var lastMonth = DateTime.Now.AddMonths(-1);
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var stats = new DoctorDashboardStatsDto
            {
                TotalPatients = await _dbContext.Patients.CountAsync(p => p.DoctorId == doctor.Id),

                ReportsThisMonth = await _dbContext.MedicalReports.Where(r => r.MedicalRecord.Patient.DoctorId == doctor.Id && r.CreatedAt >= monthStart).CountAsync(),

                ReportsToday = await _dbContext.MedicalReports.Where(r => r.MedicalRecord.Patient.DoctorId == doctor.Id && r.CreatedAt.Date == now.Date).CountAsync(),

                MonthlyReportTrend = await GetMonthlyReportTrend(doctor.Id, 6),

                BloodTypeDistribution = await _dbContext.Patients
                    .Where(p => p.DoctorId == doctor.Id && p.BloodType != null)
                    .GroupBy(p => p.BloodType)
                    .Select(g => new BloodTypeCountDto
                    {
                        BloodType = g.Key,
                        Count = g.Count()
                    }).ToListAsync(),

                AvgReportsPerPatient = await _dbContext.Patients
                    .Where(p=>p.DoctorId==doctor.Id)
                    .Select(p=>p.MedicalRecord.MedicalReports.Count)
                    .AverageAsync()

            };


            return Ok(stats);
        }

        private async Task<List<MonthlyTrendDto>> GetMonthlyReportTrend(int doctorId, int months)
        {
            var startDate = DateTime.Now.AddMonths(-months);

            var results = await _dbContext.MedicalReports.Where(r => r.MedicalRecord.Patient.DoctorId == doctorId && r.CreatedAt >= startDate)
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return results.Select(x => new MonthlyTrendDto
            {
                Year = x.Year,
                Month = $"{x.Year}-{x.Month:00}",
                Count = x.Count
            })
            .ToList();
        }
    }
}
