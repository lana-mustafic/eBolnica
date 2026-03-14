using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eBolnicaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly AppDbContext _dbContext;
        public FileController(IFileService fileService, AppDbContext dbcontext)
        {
            _fileService = fileService;
            _dbContext = dbcontext;
        }

        private async Task<Doctor?> GetCurrentDoctor()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _dbContext.Doctors.FirstOrDefaultAsync(d => d.AppUserId == userId);
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] int patientId)
        {
            try
            {
                var doctor = await GetCurrentDoctor();
                if (doctor == null) return Forbid();

                var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
                if (patient == null) return NotFound("Patient not found");
                if (patient.DoctorId != doctor.Id) return Forbid();

                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("File type not allowed");

                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest("File size exceeds limit (10MB)");

                var result = await _fileService.UploadFileAsync(file, patientId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }

        [HttpGet("download/{id}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                var doctor = await GetCurrentDoctor();
                if (doctor == null) return Forbid();

                var file = await _dbContext.Files.Include(f => f.Patient).FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return NotFound("File not found");
                if (file.Patient.DoctorId != doctor.Id) return Forbid();

                var (fileBytes, contentType, fileName) = await _fileService.DownloadFileAsync(id);
                return File(fileBytes, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error downloading file: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                var doctor = await GetCurrentDoctor();
                if (doctor == null) return Forbid();

                var file = await _dbContext.Files.Include(f => f.Patient).FirstOrDefaultAsync(f => f.Id == id);
                if (file == null) return NotFound("File not found");
                if (file.Patient.DoctorId != doctor.Id) return Forbid();

                var result = await _fileService.DeleteFileAsync(id);

                if (!result)
                    return NotFound("File not found");

                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting file: {ex.Message}");
            }
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetPatientFiles(int patientId)
        {
            try
            {
                var doctor = await GetCurrentDoctor();
                if (doctor == null) return Forbid();

                var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
                if (patient == null) return NotFound("Patient not found");
                if (patient.DoctorId != doctor.Id) return Forbid();

                var files = await _fileService.GetPatientFilesAsync(patientId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving files: {ex.Message}");
            }

        }
    }
}
