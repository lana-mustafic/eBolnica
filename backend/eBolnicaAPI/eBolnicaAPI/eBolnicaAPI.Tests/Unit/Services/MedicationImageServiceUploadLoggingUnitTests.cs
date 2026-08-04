using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.Services;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImageServiceUploadLoggingUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ListLogger<MedicationImageService> _logger = new();
        private readonly MedicationImageOptimizer _optimizer;
        private readonly MedicationImageStorageService _storageService;
        private readonly string _contentRoot;

        public MedicationImageServiceUploadLoggingUnitTests()
        {
            _contentRoot = Path.Combine(Path.GetTempPath(), "eBolnica-upload-log-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_contentRoot);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _context.Medications.Add(new Medication
            {
                Id = 7,
                Name = "Test Med",
                NormalizedName = "test med",
                Category = "Test",
                Price = 10,
                StockQuantity = 1,
                MinimumStockLevel = 1,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                RequiresPrescription = false
            });
            _context.SaveChanges();

            var settings = Options.Create(new MedicationImageUploadSettings
            {
                MaxWidth = 1920,
                MaxHeight = 1920,
                ThumbnailWidth = 128,
                ThumbnailHeight = 128
            });

            _optimizer = new MedicationImageOptimizer(settings);

            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(_contentRoot);
            _storageService = new MedicationImageStorageService(
                env.Object,
                new MedicationImageThumbnailGenerator(settings),
                settings);
        }

        [Fact]
        public async Task UploadImageAsync_LogsOriginalAndOptimizedByteSizes()
        {
            var service = CreateService();
            await using var source = await CreateJpegStreamAsync(4000, 3000);
            var sourceBytes = source.ToArray();
            var originalBytes = sourceBytes.Length;
            var file = CreateFormFile(sourceBytes, "large-photo.jpg");

            await service.UploadImageAsync(7, file);

            var logMessage = Assert.Single(_logger.Messages);
            Assert.Contains($"OriginalBytes={originalBytes}", logMessage);
            Assert.Contains("OptimizedBytes=", logMessage);
            Assert.Contains("BytesSaved=", logMessage);
            Assert.Contains("OriginalWidth=4000", logMessage);
            Assert.Contains("OriginalHeight=3000", logMessage);
            Assert.Contains("OptimizedWidth=1920", logMessage);
        Assert.Contains("OptimizedHeight=1440", logMessage);
    }

    [Fact]
    public async Task UploadImageAsync_CorruptJpeg_ThrowsSecurityException()
    {
        var service = CreateService();
        var corruptBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00 };
        var file = CreateFormFile(corruptBytes, "corrupt.jpg");

        var exception = await Assert.ThrowsAsync<MedicationImageSecurityException>(
            () => service.UploadImageAsync(7, file));

        Assert.Contains("unsupported or malformed image format", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
        {
            _context.Dispose();

            if (Directory.Exists(_contentRoot))
            {
                Directory.Delete(_contentRoot, recursive: true);
            }
        }

        private MedicationImageService CreateService()
        {
            return new MedicationImageService(
                _context,
                new MedicationImageFileValidator(Options.Create(new MedicationImageUploadSettings())),
                new MedicationImageVirusScanner(),
                _optimizer,
                _storageService,
                _logger);
        }

        private static IFormFile CreateFormFile(byte[] content, string fileName)
        {
            var file = new Mock<IFormFile>();
            file.SetupGet(f => f.Length).Returns(content.Length);
            file.SetupGet(f => f.FileName).Returns(fileName);
            file.SetupGet(f => f.ContentType).Returns("image/jpeg");
            file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content, writable: false));
            file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>(async (target, _) =>
                {
                    await target.WriteAsync(content);
                });
            return file.Object;
        }

        private static async Task<MemoryStream> CreateJpegStreamAsync(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height, Color.White);
            var stream = new MemoryStream();
            await image.SaveAsync(stream, new JpegEncoder());
            stream.Position = 0;
            return stream;
        }

        private sealed class ListLogger<T> : ILogger<T>
        {
            public List<string> Messages { get; } = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Messages.Add(formatter(state, exception));
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose()
                {
                }
            }
        }
    }
}
