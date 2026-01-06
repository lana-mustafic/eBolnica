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
            var records = await _dbContext.MedicalRecords.Where(p => p.PatientId == id).Select(p => new MedicalRecordDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                RecordNumber = p.RecordNumber,
            }).FirstOrDefaultAsync();

            return Ok(records);
        }
    }
}
