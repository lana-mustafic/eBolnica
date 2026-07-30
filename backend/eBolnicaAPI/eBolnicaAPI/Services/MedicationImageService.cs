using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace eBolnicaAPI.Services
{
    public class MedicationImageService : IMedicationImageService
    {
        private readonly AppDbContext _context;
        private readonly IMedicationImageFileValidator _fileValidator;
        private readonly IMedicationImageVirusScanner _virusScanner;
        private readonly IMedicationImageOptimizer _imageOptimizer;
        private readonly IMedicationImageStorageService _storageService;
        private readonly ILogger<MedicationImageService> _logger;

        public MedicationImageService(
            AppDbContext context,
            IMedicationImageFileValidator fileValidator,
            IMedicationImageVirusScanner virusScanner,
            IMedicationImageOptimizer imageOptimizer,
            IMedicationImageStorageService storageService,
            ILogger<MedicationImageService> logger)
        {
            _context = context;
            _fileValidator = fileValidator;
            _virusScanner = virusScanner;
            _imageOptimizer = imageOptimizer;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<List<MedicationImageDto>> GetImagesAsync(int medicationId)
        {
            await GetMedicationOrThrow(medicationId);

            var images = await _context.MedicationImages
                .AsNoTracking()
                .Where(i => i.MedicationId == medicationId)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .ToListAsync();

            return images.Select(MapToDto).ToList();
        }

        public async Task<MedicationImageDto> UploadImageAsync(int medicationId, IFormFile file)
        {
            var medication = await GetMedicationOrThrow(medicationId);
            _fileValidator.ValidateUpload(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var sanitizedFileName = _fileValidator.SanitizeOriginalFileName(file.FileName);

            await using var uploadStream = new MemoryStream();
            await file.CopyToAsync(uploadStream);
            var originalBytes = uploadStream.Length;

            await _virusScanner.ScanAsync(uploadStream);

            uploadStream.Position = 0;
            var originalImageInfo = await Image.IdentifyAsync(uploadStream);
            uploadStream.Position = 0;

            ProcessedImageResult optimizedImage;
            try
            {
                optimizedImage = await _imageOptimizer.OptimizeAsync(uploadStream, extension);
            }
            catch (UnknownImageFormatException)
            {
                throw new MedicationImageValidationException("Uploaded file is not a valid image.");
            }
            catch (InvalidImageContentException)
            {
                throw new MedicationImageValidationException("Uploaded image content is corrupted or unsupported.");
            }

            using (optimizedImage)
            {
                var optimizedBytes = optimizedImage.Length;
                var bytesSaved = MedicationImageUploadLog.CalculateBytesSaved(originalBytes, optimizedBytes);
                var storedImage = await _storageService.SaveAsync(
                    medicationId,
                    optimizedImage.Content,
                    optimizedImage.Extension);

                var existingCount = await _context.MedicationImages.CountAsync(i => i.MedicationId == medicationId);
                var isPrimary = existingCount == 0;

                var image = new MedicationImage
                {
                    MedicationId = medicationId,
                    FileName = sanitizedFileName,
                    RelativeUrl = storedImage.RelativeUrl,
                    ThumbnailRelativeUrl = storedImage.ThumbnailRelativeUrl,
                    IsPrimary = isPrimary,
                    SortOrder = existingCount,
                    UploadedAt = DateTime.UtcNow,
                    FileSizeBytes = optimizedImage.Length,
                    Width = optimizedImage.Width,
                    Height = optimizedImage.Height
                };

                _context.MedicationImages.Add(image);

                if (isPrimary)
                {
                    medication.ImageUrl = GetListDisplayUrl(image);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Medication image uploaded. MedicationId={MedicationId}, FileName={FileName}, OriginalBytes={OriginalBytes}, OptimizedBytes={OptimizedBytes}, BytesSaved={BytesSaved}, OriginalWidth={OriginalWidth}, OriginalHeight={OriginalHeight}, OptimizedWidth={OptimizedWidth}, OptimizedHeight={OptimizedHeight}, ImageUrl={ImageUrl}, ThumbnailUrl={ThumbnailUrl}",
                    medicationId,
                    sanitizedFileName,
                    originalBytes,
                    optimizedBytes,
                    bytesSaved,
                    originalImageInfo?.Width,
                    originalImageInfo?.Height,
                    optimizedImage.Width,
                    optimizedImage.Height,
                    storedImage.RelativeUrl,
                    storedImage.ThumbnailRelativeUrl);

                return MapToDto(image);
            }
        }

        public async Task SetPrimaryImageAsync(int medicationId, int imageId)
        {
            var medication = await GetMedicationOrThrow(medicationId);

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

            medication.ImageUrl = GetListDisplayUrl(target);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteImageAsync(int medicationId, int imageId)
        {
            var medication = await GetMedicationOrThrow(medicationId);

            var image = await _context.MedicationImages
                .FirstOrDefaultAsync(i => i.MedicationId == medicationId && i.Id == imageId);

            if (image == null)
            {
                throw new KeyNotFoundException("Image not found");
            }

            var wasPrimary = image.IsPrimary;
            _storageService.Delete(image.RelativeUrl, image.ThumbnailRelativeUrl);
            _context.MedicationImages.Remove(image);

            if (wasPrimary)
            {
                var nextPrimary = await _context.MedicationImages
                    .Where(i => i.MedicationId == medicationId && i.Id != imageId)
                    .OrderBy(i => i.SortOrder)
                    .FirstOrDefaultAsync();

                if (nextPrimary != null)
                {
                    nextPrimary.IsPrimary = true;
                    medication.ImageUrl = GetListDisplayUrl(nextPrimary);
                }
                else
                {
                    medication.ImageUrl = null;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<string?> GetPrimaryImageUrlAsync(int medicationId)
        {
            return await _context.Medications
                .AsNoTracking()
                .Where(m => m.Id == medicationId)
                .Select(m => m.ImageUrl)
                .FirstOrDefaultAsync();
        }

        private async Task<Medication> GetMedicationOrThrow(int medicationId)
        {
            var medication = await _context.Medications.FindAsync(medicationId);
            if (medication == null)
            {
                throw new KeyNotFoundException("Medication not found");
            }

            return medication;
        }

        private static string GetListDisplayUrl(MedicationImage image)
        {
            return image.ThumbnailRelativeUrl ?? image.RelativeUrl;
        }

        private MedicationImageDto MapToDto(MedicationImage image)
        {
            var fileSizeBytes = image.FileSizeBytes;
            var width = image.Width;
            var height = image.Height;

            if (fileSizeBytes is null || width is null || height is null)
            {
                var metadata = _storageService.TryGetFileMetadata(image.RelativeUrl);
                if (metadata != null)
                {
                    fileSizeBytes ??= metadata.FileSizeBytes;
                    width ??= metadata.Width;
                    height ??= metadata.Height;
                }
            }

            return new MedicationImageDto
            {
                Id = image.Id,
                MedicationId = image.MedicationId,
                FileName = image.FileName,
                ImageUrl = image.RelativeUrl,
                ThumbnailUrl = image.ThumbnailRelativeUrl,
                IsPrimary = image.IsPrimary,
                SortOrder = image.SortOrder,
                UploadedAt = image.UploadedAt,
                FileSizeBytes = fileSizeBytes,
                Width = width,
                Height = height
            };
        }
    }
}
