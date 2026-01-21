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
    /// Professional PDF document for inventory reports
    /// </summary>
    public class InventoryPdfDocument : IDocument
    {
        private readonly List<Medication> _medications;
        private readonly PharmacyPdfReportRequest _request;
        private readonly PdfGenerationSettings _settings;
        private readonly PharmacyInfo _pharmacyInfo;

        public InventoryPdfDocument(
            List<Medication> medications,
            PharmacyPdfReportRequest request,
            PdfGenerationSettings settings,
            PharmacyInfo? pharmacyInfo = null)
        {
            _medications = medications;
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
                        .Element(compose =>
                        {
                            new PharmacyHeaderComponent(_pharmacyInfo, _settings).Compose(compose);
                        });

                    // Content
                    page.Content()
                        .Element(ComposeContent);

                    // Footer (repeated on every page)
                    page.Footer()
                        .Element(compose =>
                        {
                            new FooterComponent(_settings).Compose(compose);
                        });
                });
        }

        private void ComposeContent(IContainer container)
        {
            container
                .Column(column =>
                {
                    // Report metadata
                    column.Item()
                        .Element(compose =>
                        {
                            new ReportMetadataComponent(
                                "Pharmacy Inventory Report",
                                _request,
                                DateTime.Now,
                                _settings,
                                _medications.Count).Compose(compose);
                        });

                    // Data table
                    column.Item()
                        .PaddingTop(20)
                        .Element(compose =>
                        {
                            new InventoryTableComponent(_medications, _settings).Compose(compose);
                        });

                    // Summary statistics
                    column.Item()
                        .PaddingTop(30)
                        .Element(compose =>
                        {
                            new SummaryStatisticsComponent(_medications, _settings).Compose(compose);
                        });

                    // Notes section
                    column.Item()
                        .PaddingTop(30)
                        .Element(compose =>
                        {
                            new NotesComponent(_settings, isInventoryReport: true).Compose(compose);
                        });
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
