using eBolnicaAPI.Models;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// Professional PDF document for prescriptions reports
    /// </summary>
    public class PrescriptionsPdfDocument : IDocument
    {
        private readonly List<Prescription> _prescriptions;
        private readonly PharmacyPdfReportRequest _request;
        private readonly PdfGenerationSettings _settings;
        private readonly PharmacyInfo _pharmacyInfo;

        public PrescriptionsPdfDocument(
            List<Prescription> prescriptions,
            PharmacyPdfReportRequest request,
            PdfGenerationSettings settings,
            PharmacyInfo? pharmacyInfo = null)
        {
            _prescriptions = prescriptions;
            _request = request;
            _settings = settings;
            _pharmacyInfo = pharmacyInfo ?? new PharmacyInfo();
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    // Page size and margins
                    page.Size(GetPageSize(_settings.DefaultPageSize));
                    page.Margin(_settings.MarginMillimeters, Unit.Millimetre);

                    // Header (repeated on every page)
                    page.Header()
                        .Element(new PharmacyHeaderComponent(_pharmacyInfo, _settings));

                    // Content
                    page.Content()
                        .Element(ComposeContent);

                    // Footer (repeated on every page)
                    page.Footer()
                        .Element(new FooterComponent(_settings));
                });
        }

        private void ComposeContent(IContainer container)
        {
            container
                .Column(column =>
                {
                    // Report metadata
                    column.Item()
                        .Element(new ReportMetadataComponent(
                            "Pharmacy Prescriptions Report",
                            _request,
                            DateTime.Now,
                            _settings,
                            _prescriptions.Count));

                    // Data table
                    column.Item()
                        .PaddingTop(20)
                        .Element(new PrescriptionsTableComponent(_prescriptions, _settings));

                    // Notes section
                    column.Item()
                        .PaddingTop(30)
                        .Element(new NotesComponent(_settings, isInventoryReport: false));
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
