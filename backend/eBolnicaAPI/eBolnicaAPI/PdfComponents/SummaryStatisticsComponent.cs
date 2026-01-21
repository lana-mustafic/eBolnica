using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
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

                    // Total Items
                    column.Item()
                        .PaddingTop(10)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Total Items:")
                                .FontSize(10);
                            row.RelativeItem()
                                .AlignRight()
                                .Text(_medications.Count.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.PrimaryColor);
                        });

                    // Total Value
                    column.Item()
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Total Value:")
                                .FontSize(10);
                            row.RelativeItem()
                                .AlignRight()
                                .Text($"{totalValue:C}")
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.PrimaryColor);
                        });

                    // Expiring Soon
                    column.Item()
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Expiring Soon (<30 days):")
                                .FontSize(10);
                            row.RelativeItem()
                                .AlignRight()
                                .Text(expiringSoon.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.WarningColor);
                        });

                    // Expired
                    column.Item()
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Expired:")
                                .FontSize(10);
                            row.RelativeItem()
                                .AlignRight()
                                .Text(expired.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.DangerColor);
                        });

                    // Out of Stock
                    column.Item()
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Out of Stock:")
                                .FontSize(10);
                            row.RelativeItem()
                                .AlignRight()
                                .Text(outOfStock.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.DangerColor);
                        });

                    // Low Stock
                    column.Item()
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text("Low Stock:")
                                .FontSize(10);
                            row.RelativeItem()
                                .AlignRight()
                                .Text(lowStock.ToString())
                                .Bold()
                                .FontSize(10)
                                .FontColor(PharmacyPdfTheme.WarningColor);
                        });
                });
        }
    }
}
