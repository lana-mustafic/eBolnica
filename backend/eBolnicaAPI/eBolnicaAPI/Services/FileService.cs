using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Services
{
    public interface IFileService
    {
        Task<FileDownloadDto> UploadFileAsync(IFormFile file, int patientId);
        Task<(byte[] fileBytes, string contentType, string fileName)> DownloadFileAsync(int fileId);
        Task<List<FileDownloadDto>> GetPatientFilesAsync(int patientId);
        Task<bool> DeleteFileAsync(int fileId);
    }

    public class FileService: IFileService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _uploadPath;

        public FileService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _uploadPath = Path.Combine(_env.ContentRootPath, "Uploads");

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<FileDownloadDto> UploadFileAsync(IFormFile file, int patientId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Generiši jedinstveno ime fajla
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(_uploadPath, fileName);

            // Sačuvaj fajl na disk
            using(var stream = new FileStream(filePath, FileMode.Create)) 
            { 
                await file.CopyToAsync(stream);
            }

            // Sačuvaj u bazu
            var fileEntity = new FileEntity
            {
                FileName = file.FileName,
                FilePath = filePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.Now,
                PatientId = patientId,
            };

            _context.Files.Add(fileEntity);
            await _context.SaveChangesAsync();

            return new FileDownloadDto
            {
                Id = fileEntity.Id,
                FileName = fileEntity.FileName,
                FilePath = fileEntity.FilePath,
                ContentType = fileEntity.ContentType,
                FileSize = fileEntity.FileSize,
                UploadedAt = fileEntity.UploadedAt,
                PatientId = fileEntity.PatientId  
            };
        }

        public async Task<(byte[] fileBytes, string contentType, string fileName)> DownloadFileAsync(int fileId)
        {
            var fileEntity = await _context.Files.FindAsync(fileId);

            if (fileEntity == null)
                throw new FileNotFoundException("File not found");

            if (!File.Exists(fileEntity.FilePath))
                throw new FileNotFoundException("Physical file not found");

            var fileBytes = await File.ReadAllBytesAsync(fileEntity.FilePath);

            return(fileBytes, fileEntity.ContentType, fileEntity.FileName);
        }

        public async Task<List<FileDownloadDto>> GetPatientFilesAsync(int patientId)
        {
            return await _context.Files
                .Where(f => f.PatientId == patientId)
                .OrderByDescending(f => f.UploadedAt) // ← Sortiraj po datumu
                .Select(f => new FileDownloadDto
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    ContentType = f.ContentType,
                    FileSize = f.FileSize,
                    UploadedAt = f.UploadedAt,
                    PatientId = f.PatientId
                })
                .ToListAsync();
        }

        public async Task<bool> DeleteFileAsync(int fileId)
        {
            var fileEntity = await _context.Files.FindAsync(fileId);

            if (fileEntity == null)
                return false;

            if (File.Exists(fileEntity.FilePath))
            {
                File.Delete(fileEntity.FilePath);
            }

            _context.Files.Remove(fileEntity);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
