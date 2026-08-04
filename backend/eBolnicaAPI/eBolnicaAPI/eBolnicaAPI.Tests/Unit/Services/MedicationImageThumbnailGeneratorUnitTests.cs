using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImageThumbnailGeneratorUnitTests
    {
        [Fact]
        public async Task GenerateAsync_4000x3000Source_Produces128x128JpegPerSettings()
        {
            var generator = CreateGenerator(new MedicationImageUploadSettings
            {
                ThumbnailWidth = 128,
                ThumbnailHeight = 128,
                ThumbnailJpegQuality = 80
            });

            await using var input = await CreateJpegStreamAsync(4000, 3000);
            using var thumbnail = await generator.GenerateAsync(input);

            AssertJpegStream(thumbnail, 128, 128);
        }

        [Fact]
        public async Task GenerateAsync_PortraitSource_ProducesSquare128x128Crop()
        {
            var generator = CreateGenerator(new MedicationImageUploadSettings
            {
                ThumbnailWidth = 128,
                ThumbnailHeight = 128
            });

            await using var input = await CreateJpegStreamAsync(900, 1600);
            using var thumbnail = await generator.GenerateAsync(input);

            AssertJpegStream(thumbnail, 128, 128);
        }

        [Fact]
        public async Task GenerateAsync_SmallSource_StillProduces128x128Crop()
        {
            var generator = CreateGenerator(new MedicationImageUploadSettings
            {
                ThumbnailWidth = 128,
                ThumbnailHeight = 128
            });

            await using var input = await CreateJpegStreamAsync(64, 64);
            using var thumbnail = await generator.GenerateAsync(input);

            AssertJpegStream(thumbnail, 128, 128);
        }

        [Fact]
        public async Task GenerateAsync_UsesConfiguredThumbnailDimensionsFromSettings()
        {
            var generator = CreateGenerator(new MedicationImageUploadSettings
            {
                ThumbnailWidth = 96,
                ThumbnailHeight = 96
            });

            await using var input = await CreateJpegStreamAsync(800, 600);
            using var thumbnail = await generator.GenerateAsync(input);

            AssertJpegStream(thumbnail, 96, 96);
        }

        private static MedicationImageThumbnailGenerator CreateGenerator(MedicationImageUploadSettings settings)
        {
            return new MedicationImageThumbnailGenerator(Options.Create(settings));
        }

        private static async Task<MemoryStream> CreateJpegStreamAsync(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height, Color.White);
            var stream = new MemoryStream();
            await image.SaveAsync(stream, new JpegEncoder());
            stream.Position = 0;
            return stream;
        }

        private static void AssertJpegStream(MemoryStream thumbnail, int expectedWidth, int expectedHeight)
        {
            Assert.True(thumbnail.Length > 0);

            var header = thumbnail.ToArray()[..3];
            Assert.Equal(0xFF, header[0]);
            Assert.Equal(0xD8, header[1]);
            Assert.Equal(0xFF, header[2]);

            thumbnail.Position = 0;
            using var image = Image.Load(thumbnail);
            Assert.Equal(expectedWidth, image.Width);
            Assert.Equal(expectedHeight, image.Height);
        }
    }
}
