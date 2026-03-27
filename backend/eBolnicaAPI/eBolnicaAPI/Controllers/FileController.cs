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

                var patient = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.DoctorId == doctor.Id);
                if (patient == null) return NotFound("Patient not found or access denied");

                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
                var allowedContentTypes = new[]
                {
                    "application/pdf",
                    "image/jpeg",
                    "image/png",
                    "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("File type not allowed");

                if (!allowedContentTypes.Contains(file.ContentType))
                    return BadRequest("File content type not allowed");

                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest("File size exceeds limit (10MB)");

                var result = await _fileService.UploadFileAsync(file, patientId);

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request." });
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

                var file = await _dbContext.Files
                .Include(f => f.Patient)
                .FirstOrDefaultAsync(f => f.Id == id && f.Patient.DoctorId == doctor.Id);
                if (file == null) return NotFound("File not found or access denied");

                var (fileBytes, contentType, fileName) = await _fileService.DownloadFileAsync(id);
                return File(fileBytes, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found");
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request." });
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

                var file = await _dbContext.Files
                 .Include(f => f.Patient)
                 .FirstOrDefaultAsync(f => f.Id == id && f.Patient.DoctorId == doctor.Id);
                if (file == null) return NotFound("File not found or access denied");

                var result = await _fileService.DeleteFileAsync(id);

                if (!result)
                    return NotFound("File not found");

                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request." });
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

                var patient = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && p.DoctorId == doctor.Id);
                if (patient == null) return NotFound("Patient not found or access denied");

                var files = await _fileService.GetPatientFilesAsync(patientId);
                return Ok(files);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }

        }
    }
}
