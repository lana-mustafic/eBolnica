using eBolnicaAPI.Models.Settings;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    /// <summary>
    /// Stores medication images in /uploads/medications/{medicationId}/original/ and /thumbnails/.
    /// </summary>
    public class MedicationImageStorageService : IMedicationImageStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMedicationImageThumbnailGenerator _thumbnailGenerator;
        private readonly MedicationImageUploadSettings _settings;
        private readonly string _uploadRoot;

        public MedicationImageStorageService(
            IWebHostEnvironment env,
            IMedicationImageThumbnailGenerator thumbnailGenerator,
            IOptions<MedicationImageUploadSettings> settings)
        {
            _env = env;
            _thumbnailGenerator = thumbnailGenerator;
            _settings = settings.Value;
            _uploadRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads", _settings.UploadSubDirectory));

            Directory.CreateDirectory(_uploadRoot);
        }

        public void EnsureFolderStructure(int medicationId)
        {
            var medicationFolder = GetSecureMedicationFolder(medicationId);
            Directory.CreateDirectory(GetOriginalFolder(medicationId));
            Directory.CreateDirectory(GetThumbnailsFolder(medicationId));
            EnsurePathIsWithinRoot(medicationFolder, _uploadRoot);
        }

        public async Task<StoredMedicationImageResult> SaveAsync(int medicationId, Stream content, string extension)
        {
            if (medicationId <= 0)
            {
                throw new MedicationImageValidationException("Invalid medication identifier.");
            }

            EnsureFolderStructure(medicationId);

            var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(storedFileName)}.jpg";

            var originalFolder = GetOriginalFolder(medicationId);
            var thumbnailsFolder = GetThumbnailsFolder(medicationId);

            var originalAbsolutePath = Path.GetFullPath(Path.Combine(originalFolder, storedFileName));
            var thumbnailAbsolutePath = Path.GetFullPath(Path.Combine(thumbnailsFolder, thumbnailFileName));

            EnsurePathIsWithinRoot(originalAbsolutePath, GetSecureMedicationFolder(medicationId));
            EnsurePathIsWithinRoot(thumbnailAbsolutePath, GetSecureMedicationFolder(medicationId));

            await using (var fileStream = new FileStream(
                originalAbsolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                content.Position = 0;
                await content.CopyToAsync(fileStream);
            }

            content.Position = 0;
            using var thumbnailStream = await _thumbnailGenerator.GenerateAsync(content);
            await using (var thumbnailFileStream = new FileStream(
                thumbnailAbsolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await thumbnailStream.CopyToAsync(thumbnailFileStream);
            }

            var relativeUrl = BuildRelativeUrl(medicationId, MedicationImageFolders.Original, storedFileName);
            var thumbnailRelativeUrl = BuildRelativeUrl(medicationId, MedicationImageFolders.Thumbnails, thumbnailFileName);

            return new StoredMedicationImageResult
            {
                RelativeUrl = relativeUrl,
                ThumbnailRelativeUrl = thumbnailRelativeUrl,
                StoredFileName = storedFileName
            };
        }

        public void Delete(string originalRelativeUrl, string? thumbnailRelativeUrl = null)
        {
            DeleteIfExists(originalRelativeUrl);

            if (!string.IsNullOrWhiteSpace(thumbnailRelativeUrl))
            {
                DeleteIfExists(thumbnailRelativeUrl);
            }
        }

        private void DeleteIfExists(string relativeUrl)
        {
            var absolutePath = GetSecureAbsolutePath(relativeUrl);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }

        public string GetSecureAbsolutePath(string relativeUrl)
        {
            var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var absolutePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, relativePath));
            EnsurePathIsWithinRoot(absolutePath, _uploadRoot);
            return absolutePath;
        }

        public MedicationImageFileMetadata? TryGetFileMetadata(string relativeUrl)
        {
            try
            {
                var absolutePath = GetSecureAbsolutePath(relativeUrl);
                if (!File.Exists(absolutePath))
                {
                    return null;
                }

                var fileInfo = new FileInfo(absolutePath);
                using var stream = fileInfo.OpenRead();
                var imageInfo = Image.Identify(stream);

                return new MedicationImageFileMetadata
                {
                    FileSizeBytes = fileInfo.Length,
                    Width = imageInfo?.Width ?? 0,
                    Height = imageInfo?.Height ?? 0
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetSecureMedicationFolder(int medicationId)
        {
            var folder = Path.GetFullPath(Path.Combine(_uploadRoot, medicationId.ToString()));
            EnsurePathIsWithinRoot(folder, _uploadRoot);
            return folder;
        }

        private string GetOriginalFolder(int medicationId)
        {
            return Path.Combine(GetSecureMedicationFolder(medicationId), MedicationImageFolders.Original);
        }

        private string GetThumbnailsFolder(int medicationId)
        {
            return Path.Combine(GetSecureMedicationFolder(medicationId), MedicationImageFolders.Thumbnails);
        }

        private string BuildRelativeUrl(int medicationId, string subFolder, string fileName)
        {
            return $"/uploads/{_settings.UploadSubDirectory}/{medicationId}/{subFolder}/{fileName}";
        }

        private static void EnsurePathIsWithinRoot(string targetPath, string rootPath)
        {
            var normalizedRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!targetPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new MedicationImageSecurityException("Invalid storage path detected.");
            }
        }
    }
}
