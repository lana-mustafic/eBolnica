using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImageOptimizerUnitTests
    {
        [Fact]
        public async Task OptimizeAsync_4000x3000Jpeg_ResizesToMax1920x1920PerSettings()
        {
            var optimizer = CreateOptimizer(new MedicationImageUploadSettings
            {
                MaxWidth = 1920,
                MaxHeight = 1920,
                JpegQuality = 85
            });

            await using var input = await CreateJpegStreamAsync(4000, 3000);
            using var result = await optimizer.OptimizeAsync(input, ".jpg");

            Assert.Equal(".jpg", result.Extension);
            Assert.Equal("image/jpeg", result.ContentType);
            Assert.True(result.Width <= 1920);
            Assert.True(result.Height <= 1920);
            Assert.Equal(1920, result.Width);
            Assert.Equal(1440, result.Height);
        }

        [Fact]
        public async Task OptimizeAsync_3000x4000Jpeg_ResizesPortraitImageWithin1920x1920Box()
        {
            var optimizer = CreateOptimizer(new MedicationImageUploadSettings
            {
                MaxWidth = 1920,
                MaxHeight = 1920
            });

            await using var input = await CreateJpegStreamAsync(3000, 4000);
            using var result = await optimizer.OptimizeAsync(input, ".jpg");

            Assert.True(result.Width <= 1920);
            Assert.True(result.Height <= 1920);
            Assert.Equal(1440, result.Width);
            Assert.Equal(1920, result.Height);
        }

        [Fact]
        public async Task OptimizeAsync_ImageWithinLimits_PreservesDimensions()
        {
            var optimizer = CreateOptimizer(new MedicationImageUploadSettings
            {
                MaxWidth = 1920,
                MaxHeight = 1920
            });

            await using var input = await CreateJpegStreamAsync(1280, 720);
            using var result = await optimizer.OptimizeAsync(input, ".jpg");

            Assert.Equal(1280, result.Width);
            Assert.Equal(720, result.Height);
        }

        [Fact]
        public async Task OptimizeAsync_UsesConfiguredMaxWidthAndMaxHeightFromSettings()
        {
            var optimizer = CreateOptimizer(new MedicationImageUploadSettings
            {
                MaxWidth = 800,
                MaxHeight = 600
            });

            await using var input = await CreateJpegStreamAsync(1600, 1200);
            using var result = await optimizer.OptimizeAsync(input, ".jpg");

            Assert.True(result.Width <= 800);
            Assert.True(result.Height <= 600);
            Assert.Equal(800, result.Width);
            Assert.Equal(600, result.Height);
        }

        private static MedicationImageOptimizer CreateOptimizer(MedicationImageUploadSettings settings)
        {
            return new MedicationImageOptimizer(Options.Create(settings));
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
