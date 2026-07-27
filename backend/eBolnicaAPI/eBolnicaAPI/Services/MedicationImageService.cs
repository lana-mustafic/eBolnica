using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Services
{
    public class MedicationImageService : IMedicationImageService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _uploadRoot;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public MedicationImageService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _uploadRoot = Path.Combine(_env.ContentRootPath, "Uploads", "medications");

            if (!Directory.Exists(_uploadRoot))
            {
                Directory.CreateDirectory(_uploadRoot);
            }
        }

        public async Task<List<MedicationImageDto>> GetImagesAsync(int medicationId)
        {
            await EnsureMedicationExists(medicationId);

            return await _context.MedicationImages
                .AsNoTracking()
                .Where(i => i.MedicationId == medicationId)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => MapToDto(i))
                .ToListAsync();
        }

        public async Task<MedicationImageDto> UploadImageAsync(int medicationId, IFormFile file)
        {
            await EnsureMedicationExists(medicationId);
            ValidateImageFile(file);

            var medicationFolder = Path.Combine(_uploadRoot, medicationId.ToString());
            if (!Directory.Exists(medicationFolder))
            {
                Directory.CreateDirectory(medicationFolder);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var absolutePath = Path.Combine(medicationFolder, storedFileName);
            var relativeUrl = $"/uploads/medications/{medicationId}/{storedFileName}";

            await using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var existingCount = await _context.MedicationImages.CountAsync(i => i.MedicationId == medicationId);
            var isPrimary = existingCount == 0;

            var image = new MedicationImage
            {
                MedicationId = medicationId,
                FileName = file.FileName,
                RelativeUrl = relativeUrl,
                IsPrimary = isPrimary,
                SortOrder = existingCount,
                UploadedAt = DateTime.UtcNow
            };

            _context.MedicationImages.Add(image);
            await _context.SaveChangesAsync();

            return MapToDto(image);
        }

        public async Task SetPrimaryImageAsync(int medicationId, int imageId)
        {
            await EnsureMedicationExists(medicationId);

            var images = await _context.MedicationImages
                .Where(i => i.MedicationId == medicationId)
                .ToListAsync();

            var target = images.FirstOrDefault(i => i.Id == imageId);
            if (target == null)
            {
                throw new KeyNotFoundException("Image not found");
            }

            foreach (var image in images)
            {
                image.IsPrimary = image.Id == imageId;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteImageAsync(int medicationId, int imageId)
        {
            await EnsureMedicationExists(medicationId);

            var image = await _context.MedicationImages
                .FirstOrDefaultAsync(i => i.MedicationId == medicationId && i.Id == imageId);

            if (image == null)
            {
                throw new KeyNotFoundException("Image not found");
            }

            var wasPrimary = image.IsPrimary;
            DeletePhysicalFile(image.RelativeUrl);

            _context.MedicationImages.Remove(image);
            await _context.SaveChangesAsync();

            if (wasPrimary)
            {
                var nextPrimary = await _context.MedicationImages
                    .Where(i => i.MedicationId == medicationId)
                    .OrderBy(i => i.SortOrder)
                    .FirstOrDefaultAsync();

                if (nextPrimary != null)
                {
                    nextPrimary.IsPrimary = true;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<string?> GetPrimaryImageUrlAsync(int medicationId)
        {
            return await _context.MedicationImages
                .AsNoTracking()
                .Where(i => i.MedicationId == medicationId)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.RelativeUrl)
                .FirstOrDefaultAsync();
        }

        private async Task EnsureMedicationExists(int medicationId)
        {
            var exists = await _context.Medications.AnyAsync(m => m.Id == medicationId);
            if (!exists)
            {
                throw new KeyNotFoundException("Medication not found");
            }
        }

        private static void ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file uploaded");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException("File size exceeds limit (5MB)");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new ArgumentException("File type not allowed. Use JPG, PNG, or WEBP.");
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                throw new ArgumentException("File content type not allowed");
            }
        }

        private void DeletePhysicalFile(string relativeUrl)
        {
            var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var absolutePath = Path.Combine(_env.ContentRootPath, relativePath);

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }

        private static MedicationImageDto MapToDto(MedicationImage image) => new()
        {
            Id = image.Id,
            MedicationId = image.MedicationId,
            FileName = image.FileName,
            ImageUrl = image.RelativeUrl,
            IsPrimary = image.IsPrimary,
            SortOrder = image.SortOrder,
            UploadedAt = image.UploadedAt
        };
    }
}
