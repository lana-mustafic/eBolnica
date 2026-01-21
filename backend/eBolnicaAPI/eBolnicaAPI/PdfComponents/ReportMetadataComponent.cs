using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// Report metadata component with title, subtitle, and filter summary
    /// </summary>
    public class ReportMetadataComponent : IComponent
    {
        private readonly string _reportTitle;
        private readonly PharmacyPdfReportRequest? _request;
        private readonly DateTime _generationTime;
        private readonly PdfGenerationSettings _settings;
        private readonly int _itemCount;

        public ReportMetadataComponent(
            string reportTitle,
            PharmacyPdfReportRequest? request,
            DateTime generationTime,
            PdfGenerationSettings settings,
            int itemCount = 0)
        {
            _reportTitle = reportTitle;
            _request = request;
            _generationTime = generationTime;
            _settings = settings;
            _itemCount = itemCount;
        }

        public void Compose(IContainer container)
        {
            container
                .Column(column =>
                {
                    // Main title
                    column.Item()
                        .PaddingTop(20)
                        .Text(_reportTitle)
                        .FontSize(24)
                        .Bold()
                        .FontColor(PharmacyPdfTheme.PrimaryColor);

                    // Subtitle with filter summary
                    var filterSummary = GetFilterSummary();
                    if (!string.IsNullOrEmpty(filterSummary))
                    {
                        column.Item()
                            .PaddingTop(5)
                            .Text(filterSummary)
                            .FontSize(11)
                            .Italic()
                            .FontColor(PharmacyPdfTheme.SecondaryColor);
                    }

                    // Item count
                    if (_itemCount > 0)
                    {
                        column.Item()
                            .PaddingTop(5)
                            .Text($"Total Items: {_itemCount}")
                            .FontSize(11)
                            .Bold()
                            .FontColor(PharmacyPdfTheme.SecondaryColor);
                    }

                    // Generation info
                    column.Item()
                        .PaddingTop(10)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text($"Generated: {_generationTime:yyyy-MM-dd HH:mm:ss}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);

                            row.RelativeItem()
                                .AlignRight()
                                .Text($"Report ID: {Guid.NewGuid():N}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);
                        });

                    // Separator
                    column.Item()
                        .PaddingVertical(10)
                        .LineHorizontal(0.5)
                        .LineColor(Colors.Grey.Lighten2);
                });
        }

        private string GetFilterSummary()
        {
            if (_request == null) return string.Empty;

            var filters = new List<string>();

            if (!string.IsNullOrEmpty(_request.Search))
                filters.Add($"Search: \"{_request.Search}\"");

            if (!string.IsNullOrEmpty(_request.Category))
                filters.Add($"Category: {_request.Category}");

            if (_request.ExpiryBefore.HasValue)
                filters.Add($"Expires before: {_request.ExpiryBefore.Value:yyyy-MM-dd}");

            if (_request.ExpiryAfter.HasValue)
                filters.Add($"Expires after: {_request.ExpiryAfter.Value:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(_request.StockStatus))
                filters.Add($"Stock Status: {_request.StockStatus}");

            if (!string.IsNullOrEmpty(_request.Status) || !string.IsNullOrEmpty(_request.PrescriptionStatus))
                filters.Add($"Status: {_request.Status ?? _request.PrescriptionStatus}");

            return filters.Any() ? $"Filters: {string.Join(", ", filters)}" : string.Empty;
        }
    }
}
