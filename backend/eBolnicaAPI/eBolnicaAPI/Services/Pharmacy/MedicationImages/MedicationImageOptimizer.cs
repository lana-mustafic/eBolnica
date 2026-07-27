using eBolnicaAPI.Models.Settings;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public class MedicationImageOptimizer : IMedicationImageOptimizer
    {
        private readonly MedicationImageUploadSettings _settings;

        public MedicationImageOptimizer(IOptions<MedicationImageUploadSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<ProcessedImageResult> OptimizeAsync(
            Stream content,
            string extension,
            CancellationToken cancellationToken = default)
        {
            content.Position = 0;

            using var image = await Image.LoadAsync(content, cancellationToken);
            image.Mutate(x => x.AutoOrient());

            if (image.Width > _settings.MaxWidth || image.Height > _settings.MaxHeight)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(_settings.MaxWidth, _settings.MaxHeight)
                }));
            }

            var output = new MemoryStream();
            var normalizedExtension = extension.ToLowerInvariant();

            switch (normalizedExtension)
            {
                case ".png":
                    await image.SaveAsync(output, new PngEncoder
                    {
                        CompressionLevel = PngCompressionLevel.Level6
                    }, cancellationToken);
                    break;

                case ".webp":
                    await image.SaveAsync(output, new WebpEncoder
                    {
                        Quality = _settings.JpegQuality
                    }, cancellationToken);
                    break;

                default:
                    await image.SaveAsJpegAsync(output, new JpegEncoder
                    {
                        Quality = _settings.JpegQuality
                    }, cancellationToken);
                    normalizedExtension = ".jpg";
                    break;
            }

            output.Position = 0;
            var contentType = normalizedExtension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            return new ProcessedImageResult(output, normalizedExtension, contentType, image.Width, image.Height);
        }
    }
}
