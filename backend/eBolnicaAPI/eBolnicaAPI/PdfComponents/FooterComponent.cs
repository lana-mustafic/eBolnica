using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// PDF footer component with page numbers
    /// </summary>
    public class FooterComponent : IComponent
    {
        private readonly PdfGenerationSettings _settings;

        public FooterComponent(PdfGenerationSettings settings)
        {
            _settings = settings;
        }

        public void Compose(IContainer container)
        {
            container
                .Row(row =>
                {
                    // Page numbers (centered)
                    if (_settings.IncludePageNumbers)
                    {
                        row.RelativeItem()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Page ");
                                text.CurrentPageNumber();
                                text.Span(" of ");
                                text.TotalPages();
                            })
                            .FontSize(9)
                            .FontColor(Colors.Grey.Medium);
                    }

                    // Timestamp (right aligned)
                    if (_settings.IncludeGenerationTimestamp)
                    {
                        row.RelativeItem()
                            .AlignRight()
                            .Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Lighten1);
                    }
                });
        }
    }
}
