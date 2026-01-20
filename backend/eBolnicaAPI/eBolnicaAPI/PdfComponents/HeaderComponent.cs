using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// PDF header component with title and metadata
    /// </summary>
    public class HeaderComponent : IComponent
    {
        private readonly PharmacyPdfReportRequest _request;
        private readonly PdfGenerationSettings _settings;
        private readonly string _title;

        public HeaderComponent(
            PharmacyPdfReportRequest request,
            PdfGenerationSettings settings,
            string title)
        {
            _request = request;
            _settings = settings;
            _title = title;
        }

        public void Compose(IContainer container)
        {
            container
                .Column(column =>
                {
                    // Title
                    column.Item()
                        .Text(_title)
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Medium); // Using QuestPDF colors instead of hex

                    // Generation timestamp
                    if (_settings.IncludeGenerationTimestamp)
                    {
                        column.Item()
                            .Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Medium);
                    }

                    // Filter information
                    if (!string.IsNullOrEmpty(_request.Search))
                    {
                        column.Item()
                            .PaddingTop(5)
                            .Text($"Search: {_request.Search}")
                            .FontSize(10)
                            .Italic()
                            .FontColor(Colors.Grey.Darken1);
                    }

                    if (!string.IsNullOrEmpty(_request.Category))
                    {
                        column.Item()
                            .Text($"Category: {_request.Category}")
                            .FontSize(10)
                            .Italic()
                            .FontColor(Colors.Grey.Darken1);
                    }

                    // Item count
                    column.Item()
                        .PaddingTop(5)
                        .Text($"Total Items: {GetItemCount()}")
                        .FontSize(11)
                        .Bold()
                        .FontColor(Colors.Grey.Darken2);
                });
        }

        private int GetItemCount()
        {
            // This will be overridden by the table component with actual count
            // For now, return 0 as placeholder
            return 0;
        }
    }
}
