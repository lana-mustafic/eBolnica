using eBolnicaAPI.Models.Settings;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace eBolnicaAPI.Services.Pharmacy.MedicationImages
{
    public class MedicationImageThumbnailGenerator : IMedicationImageThumbnailGenerator
    {
        private readonly MedicationImageUploadSettings _settings;

        public MedicationImageThumbnailGenerator(IOptions<MedicationImageUploadSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<MemoryStream> GenerateAsync(Stream content, CancellationToken cancellationToken = default)
        {
            content.Position = 0;

            using var image = await Image.LoadAsync(content, cancellationToken);
            image.Mutate(x => x.AutoOrient());

            var width = _settings.ThumbnailWidth;
            var height = _settings.ThumbnailHeight;

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(width, height),
                Position = AnchorPositionMode.Center
            }));

            var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder
            {
                Quality = _settings.ThumbnailJpegQuality
            }, cancellationToken);

            output.Position = 0;
            return output;
        }
    }
}
