using System.Text;
using eBolnicaAPI.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public class MedicationImageFileValidator : IMedicationImageFileValidator
    {
        private static readonly Dictionary<string, byte[][]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".webp"] = new[] { Encoding.ASCII.GetBytes("RIFF") }
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly MedicationImageUploadSettings _settings;

        public MedicationImageFileValidator(IOptions<MedicationImageUploadSettings> settings)
        {
            _settings = settings.Value;
        }

        public void ValidateUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new MedicationImageValidationException("No file uploaded.");
            }

            if (file.Length > _settings.MaxFileSizeBytes)
            {
                throw new MedicationImageValidationException(
                    $"File size exceeds limit ({_settings.MaxFileSizeBytes / (1024 * 1024)}MB).");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                throw new MedicationImageValidationException("File type not allowed. Use JPG, PNG, or WEBP.");
            }

            if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
            {
                throw new MedicationImageValidationException("File content type not allowed.");
            }

            ValidateMagicBytes(file, extension);
        }

        public string SanitizeOriginalFileName(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeName))
            {
                return "upload";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(safeName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            return cleaned.Length > 255 ? cleaned[..255] : cleaned;
        }

        private static void ValidateMagicBytes(IFormFile file, string extension)
        {
            if (!MagicBytes.TryGetValue(extension, out var signatures))
            {
                throw new MedicationImageValidationException("Unsupported file extension.");
            }

            using var stream = file.OpenReadStream();
            var header = new byte[16];
            var read = stream.Read(header, 0, header.Length);

            if (read == 0)
            {
                throw new MedicationImageValidationException("Uploaded file is empty.");
            }

            var matchesSignature = signatures.Any(signature =>
                read >= signature.Length && header.Take(signature.Length).SequenceEqual(signature));

            if (!matchesSignature)
            {
                throw new MedicationImageValidationException("File content does not match the declared image type.");
            }

            if (extension == ".webp" && read >= 12)
            {
                var webpMarker = Encoding.ASCII.GetString(header, 8, 4);
                if (!string.Equals(webpMarker, "WEBP", StringComparison.Ordinal))
                {
                    throw new MedicationImageValidationException("Invalid WEBP file signature.");
                }
            }
        }
    }
}
