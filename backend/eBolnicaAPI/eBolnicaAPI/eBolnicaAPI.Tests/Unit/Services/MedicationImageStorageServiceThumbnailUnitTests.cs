using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImageStorageServiceThumbnailUnitTests : IDisposable
    {
        private readonly string _contentRoot;

        public MedicationImageStorageServiceThumbnailUnitTests()
        {
            _contentRoot = Path.Combine(Path.GetTempPath(), "eBolnica-thumbnail-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_contentRoot);
        }

        [Fact]
        public async Task SaveAsync_Writes128x128JpegThumbnailUnderMedicationsThumbnailsFolder()
        {
            var settings = new MedicationImageUploadSettings
            {
                ThumbnailWidth = 128,
                ThumbnailHeight = 128,
                ThumbnailJpegQuality = 80,
                UploadSubDirectory = "medications"
            };

            var service = CreateStorageService(settings);

            await using var input = await CreateJpegStreamAsync(4000, 3000);
            var result = await service.SaveAsync(42, input, ".jpg");

            Assert.StartsWith("/uploads/medications/42/thumbnails/", result.ThumbnailRelativeUrl);
            Assert.EndsWith(".jpg", result.ThumbnailRelativeUrl);

            var thumbnailPath = service.GetSecureAbsolutePath(result.ThumbnailRelativeUrl);
            Assert.True(File.Exists(thumbnailPath));

            using var thumbnail = await Image.LoadAsync(thumbnailPath);
            Assert.Equal(128, thumbnail.Width);
            Assert.Equal(128, thumbnail.Height);

            var bytes = await File.ReadAllBytesAsync(thumbnailPath);
            Assert.Equal(0xFF, bytes[0]);
            Assert.Equal(0xD8, bytes[1]);
        }

        public void Dispose()
        {
            if (Directory.Exists(_contentRoot))
            {
                Directory.Delete(_contentRoot, recursive: true);
            }
        }

        private MedicationImageStorageService CreateStorageService(MedicationImageUploadSettings settings)
        {
            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(_contentRoot);

            return new MedicationImageStorageService(
                env.Object,
                new MedicationImageThumbnailGenerator(Options.Create(settings)),
                Options.Create(settings));
        }

        private static async Task<MemoryStream> CreateJpegStreamAsync(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height, Color.White);
            var stream = new MemoryStream();
            await image.SaveAsync(stream, new JpegEncoder());
            stream.Position = 0;
            return stream;
        }
    }
}
