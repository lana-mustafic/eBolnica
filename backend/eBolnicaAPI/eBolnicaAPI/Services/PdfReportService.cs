using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Exceptions;
using eBolnicaAPI.Models.Settings;
using eBolnicaAPI.PdfComponents;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.Services
{
    /// <summary>
    /// Service for generating PDF reports
    /// </summary>
    public class PdfReportService : IPdfReportService
    {
        private readonly ILogger<PdfReportService> _logger;
        private readonly PdfGenerationSettings _settings;

        public PdfReportService(
            ILogger<PdfReportService> logger,
            IOptions<PdfGenerationSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;

            // Initialize QuestPDF license (Community license for open source)
            QuestPDF.Settings.License = LicenseType.Community;

            _logger.LogInformation("PdfReportService initialized with settings: PageSize={PageSize}, Margin={Margin}mm", 
                _settings.DefaultPageSize, _settings.MarginMillimeters);
        }

        /// <summary>
        /// Generates PDF report for inventory items
        /// </summary>
        public async Task<byte[]> GenerateInventoryPdfAsync(List<Medication> medications, PharmacyPdfReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating inventory PDF report with {Count} items", medications.Count);

                var document = new InventoryPdfDocument(medications, request, _settings);
                var pdfBytes = document.GeneratePdf();

                _logger.LogInformation("Successfully generated inventory PDF report: {Size} bytes", pdfBytes.Length);

                return await Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate inventory PDF");
                throw new PdfGenerationException("Failed to generate inventory PDF", ex);
            }
        }

        /// <summary>
        /// Generates PDF report for prescriptions
        /// </summary>
        public async Task<byte[]> GeneratePrescriptionsPdfAsync(List<Prescription> prescriptions, PharmacyPdfReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating prescriptions PDF report with {Count} items", prescriptions.Count);

                var document = new PrescriptionsPdfDocument(prescriptions, request, _settings);
                var pdfBytes = document.GeneratePdf();

                _logger.LogInformation("Successfully generated prescriptions PDF report: {Size} bytes", pdfBytes.Length);

                return await Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate prescriptions PDF");
                throw new PdfGenerationException("Failed to generate prescriptions PDF", ex);
            }
        }

        /// <summary>
        /// Generates a simple PDF for testing purposes
        /// </summary>
        public byte[] GenerateSimplePdf(string title, string content)
        {
            try
            {
                _logger.LogDebug("Generating simple test PDF: {Title}", title);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50, Unit.Point);

                        // Header
                        page.Header()
                            .Text(title)
                            .Bold()
                            .FontSize(20);

                        // Content
                        page.Content()
                            .PaddingVertical(20)
                            .Text(content);

                        // Footer
                        if (_settings.IncludePageNumbers)
                        {
                            page.Footer()
                                .AlignCenter()
                                .Text(text =>
                                {
                                    text.Span("Page ");
                                    text.CurrentPageNumber();
                                    text.Span(" of ");
                                    text.TotalPages();
                                });
                        }

                        if (_settings.IncludeGenerationTimestamp)
                        {
                            page.Footer()
                                .AlignRight()
                                .Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                                .FontSize(8);
                        }
                    });
                });

                var pdfBytes = document.GeneratePdf();

                _logger.LogDebug("Successfully generated simple PDF: {Size} bytes", pdfBytes.Length);

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate simple PDF");
                throw new PdfGenerationException("Failed to generate simple PDF", ex);
            }
        }
    }
}
