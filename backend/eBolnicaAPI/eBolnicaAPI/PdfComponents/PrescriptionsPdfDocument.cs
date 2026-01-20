using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// PDF document for prescriptions reports
    /// </summary>
    public class PrescriptionsPdfDocument : IDocument
    {
        private readonly List<Prescription> _prescriptions;
        private readonly PharmacyPdfReportRequest _request;
        private readonly PdfGenerationSettings _settings;

        public PrescriptionsPdfDocument(
            List<Prescription> prescriptions,
            PharmacyPdfReportRequest request,
            PdfGenerationSettings settings)
        {
            _prescriptions = prescriptions;
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
                        .Element(new HeaderComponent(_request, _settings, "Pharmacy Prescriptions Report"));

                    // Content
                    page.Content()
                        .Element(new PrescriptionsTableComponent(_prescriptions, _settings));

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
