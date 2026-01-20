using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// PDF document for inventory reports
    /// </summary>
    public class InventoryPdfDocument : IDocument
    {
        private readonly List<Medication> _medications;
        private readonly PharmacyPdfReportRequest _request;
        private readonly PdfGenerationSettings _settings;

        public InventoryPdfDocument(
            List<Medication> medications,
            PharmacyPdfReportRequest request,
            PdfGenerationSettings settings)
        {
            _medications = medications;
            _request = request;
            _settings = settings;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Size(GetPageSize(_settings.DefaultPageSize));
                    page.Margin(_settings.MarginMillimeters, Unit.Millimetre);

                    // Header
                    page.Header()
                        .Element(new HeaderComponent(_request, _settings, "Pharmacy Inventory Report"));

                    // Content
                    page.Content()
                        .Element(new InventoryTableComponent(_medications, _settings));

                    // Footer
                    page.Footer()
                        .Element(new FooterComponent(_settings));
                });
        }

        private PageSize GetPageSize(string pageSizeName)
        {
            return pageSizeName.ToUpper() switch
            {
                "A4" => PageSizes.A4,
                "LETTER" => PageSizes.Letter,
                "LEGAL" => PageSizes.Legal,
                _ => PageSizes.A4
            };
        }
    }
}
