using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Controllers
{
    [Route("api/patient/medical-record")]
    [ApiController]
    public class MedicalRecordController : ControllerBase
    {

        private readonly AppDbContext _dbContext;
        public MedicalRecordController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{id}/medical-records")]
        public async Task<IActionResult> GetMedicalRecordById(int id)
        {
            var patient = await _dbContext.Patients.Include(p => p.MedicalRecord).Include(p=>p.AppUser).FirstOrDefaultAsync(p=>p.Id == id);

            if (patient == null)
            {
                return NotFound("Patient not found");
            }

            if (patient.MedicalRecord == null)
            {
                return NotFound("Medical record not found");
            }

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
                Email = patient.AppUser.Email

            };

            return Ok(records);
        }
    }
}
