using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// Summary statistics component for inventory reports
    /// </summary>
    public class SummaryStatisticsComponent : IComponent
    {
        private readonly List<Medication> _medications;
        private readonly PdfGenerationSettings _settings;

        public SummaryStatisticsComponent(List<Medication> medications, PdfGenerationSettings settings)
        {
            _medications = medications;
            _settings = settings;
        }

        public void Compose(IContainer container)
        {
            var totalValue = _medications.Sum(m => m.Price * m.StockQuantity);
            var expiringSoon = _medications.Count(m =>
                m.ExpiryDate.HasValue &&
                (m.ExpiryDate.Value.Date - DateTime.Now.Date).Days < 30 &&
                (m.ExpiryDate.Value.Date - DateTime.Now.Date).Days >= 0);
            var expired = _medications.Count(m =>
                m.ExpiryDate.HasValue &&
                (m.ExpiryDate.Value.Date - DateTime.Now.Date).Days < 0);
            var outOfStock = _medications.Count(m => m.StockQuantity == 0);
            var lowStock = _medications.Count(m =>
                m.StockQuantity > 0 && m.StockQuantity <= m.MinimumStockLevel);

            container
                .Background(PharmacyPdfTheme.Table.RowAlternateColor)
                .Padding(15)
                .Border(1)
                .BorderColor(PharmacyPdfTheme.Table.BorderColor)
                .Column(column =>
                {
                    column.Item()
                        .Text("Summary Statistics")
                        .Bold()
                        .FontSize(12)
                        .FontColor(PharmacyPdfTheme.PrimaryColor);

                    column.Item()
                        .PaddingTop(10)
                        .Grid(grid =>
                        {
                            grid.Columns(4);
                            grid.Spacing(10);

                            // Total Items
                            grid.Item().Column(1)
                                .Text("Total Items:")
                                .FontSize(10);
                            grid.Item().Column(2)
                                .Text(_medications.Count.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.PrimaryColor);

                            // Total Value
                            grid.Item().Column(1)
                                .Text("Total Value:")
                                .FontSize(10);
                            grid.Item().Column(2)
                                .Text($"{totalValue:C}")
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.PrimaryColor);

                            // Expiring Soon
                            grid.Item().Column(1)
                                .Text("Expiring Soon (<30 days):")
                                .FontSize(10);
                            grid.Item().Column(2)
                                .Text(expiringSoon.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.WarningColor);

                            // Expired
                            grid.Item().Column(1)
                                .Text("Expired:")
                                .FontSize(10);
                            grid.Item().Column(2)
                                .Text(expired.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.DangerColor);

                            // Out of Stock
                            grid.Item().Column(1)
                                .Text("Out of Stock:")
                                .FontSize(10);
                            grid.Item().Column(2)
                                .Text(outOfStock.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.DangerColor);

                            // Low Stock
                            grid.Item().Column(1)
                                .Text("Low Stock:")
                                .FontSize(10);
                            grid.Item().Column(2)
                                .Text(lowStock.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.WarningColor);
                        });
                });
        }
    }
}
