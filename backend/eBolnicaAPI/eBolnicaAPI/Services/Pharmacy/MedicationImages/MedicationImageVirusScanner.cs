using System.Text;
using SixLabors.ImageSharp;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    /// <summary>
    /// Performs content-based security checks on uploaded medication images.
    /// Rejects executable signatures, known test malware markers, and non-image payloads.
    /// </summary>
    public class MedicationImageVirusScanner : IMedicationImageVirusScanner
    {
        private const string EicarSignature = "EICAR-STANDARD-ANTIVIRUS-TEST-FILE";

        private static readonly byte[][] BlockedSignatures =
        {
            new byte[] { 0x4D, 0x5A },             // PE / DOS executable
            new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, // ELF
            new byte[] { 0x25, 0x50, 0x44, 0x46 }, // PDF
            new byte[] { 0x3C, 0x3F, 0x78, 0x6D }, // <?xm (XML/HTML/SVG script vectors)
            new byte[] { 0x3C, 0x21, 0x44, 0x4F }, // <!DO (HTML)
            new byte[] { 0x3C, 0x68, 0x74, 0x6D }, // <htm
            new byte[] { 0x3C, 0x73, 0x63, 0x72 }  // <scr
        };

        public async Task ScanAsync(Stream content, CancellationToken cancellationToken = default)
        {
            if (!content.CanSeek)
            {
                throw new MedicationImageSecurityException("Unable to scan uploaded file stream.");
            }

            content.Position = 0;
            var header = new byte[512];
            var read = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

            if (read == 0)
            {
                throw new MedicationImageSecurityException("Uploaded file is empty.");
            }

            foreach (var signature in BlockedSignatures)
            {
                if (read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature))
                {
                    throw new MedicationImageSecurityException("File failed security scan: blocked content signature detected.");
                }
            }

            content.Position = 0;
            var sampleLength = (int)Math.Min(content.Length, 4096);
            var sampleBytes = new byte[sampleLength];
            _ = await content.ReadAsync(sampleBytes.AsMemory(0, sampleLength), cancellationToken);
            var sampleText = Encoding.UTF8.GetString(sampleBytes);

            if (sampleText.Contains(EicarSignature, StringComparison.Ordinal))
            {
                throw new MedicationImageSecurityException("File failed security scan: malware test signature detected.");
            }

            content.Position = 0;
            try
            {
                var imageInfo = await Image.IdentifyAsync(content, cancellationToken);
                if (imageInfo == null)
                {
                    throw new MedicationImageSecurityException("File failed security scan: payload is not a valid image.");
                }
            }
            catch (UnknownImageFormatException)
            {
                throw new MedicationImageSecurityException("File failed security scan: unsupported or malformed image format.");
            }
            catch (InvalidImageContentException)
            {
                throw new MedicationImageSecurityException("File failed security scan: corrupted image content detected.");
            }
            finally
            {
                content.Position = 0;
            }
        }
    }
}
