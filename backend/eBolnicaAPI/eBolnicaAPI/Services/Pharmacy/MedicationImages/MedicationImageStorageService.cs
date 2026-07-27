using eBolnicaAPI.Models.Settings;
using Microsoft.Extensions.Options;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    /// <summary>
    /// Stores medication images outside web root using randomized file names and path traversal protection.
    /// </summary>
    public class MedicationImageStorageService : IMedicationImageStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly MedicationImageUploadSettings _settings;
        private readonly string _uploadRoot;

        public MedicationImageStorageService(IWebHostEnvironment env, IOptions<MedicationImageUploadSettings> settings)
        {
            _env = env;
            _settings = settings.Value;
            _uploadRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads", _settings.UploadSubDirectory));

            Directory.CreateDirectory(_uploadRoot);
        }

        public async Task<StoredMedicationImageResult> SaveAsync(int medicationId, Stream content, string extension)
        {
            if (medicationId <= 0)
            {
                throw new MedicationImageValidationException("Invalid medication identifier.");
            }

            var medicationFolder = GetSecureMedicationFolder(medicationId);
            Directory.CreateDirectory(medicationFolder);

            var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var absolutePath = Path.GetFullPath(Path.Combine(medicationFolder, storedFileName));
            EnsurePathIsWithinRoot(absolutePath, medicationFolder);

            await using (var fileStream = new FileStream(
                absolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                content.Position = 0;
                await content.CopyToAsync(fileStream);
            }

            var relativeUrl = $"/uploads/{_settings.UploadSubDirectory}/{medicationId}/{storedFileName}";
            return new StoredMedicationImageResult
            {
                RelativeUrl = relativeUrl,
                StoredFileName = storedFileName
            };
        }

        public void Delete(string relativeUrl)
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

        private string GetSecureMedicationFolder(int medicationId)
        {
            var folder = Path.GetFullPath(Path.Combine(_uploadRoot, medicationId.ToString()));
            EnsurePathIsWithinRoot(folder, _uploadRoot);
            return folder;
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
