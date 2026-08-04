using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// Notes/comments component for PDF reports
    /// </summary>
    public class NotesComponent : IComponent
    {
        private readonly PdfGenerationSettings _settings;
        private readonly bool _isInventoryReport;

        public NotesComponent(PdfGenerationSettings settings, bool isInventoryReport = true)
        {
            _settings = settings;
            _isInventoryReport = isInventoryReport;
        }

        public void Compose(IContainer container)
        {
            container
                .Column(column =>
                {
                    column.Item()
                        .Text("Notes")
                        .Bold()
                        .FontSize(11)
                        .FontColor(PharmacyPdfTheme.PrimaryColor);

                    column.Item()
                        .PaddingTop(5)
                        .Background(Colors.Yellow.Lighten5)
                        .Padding(10)
                        .Border(1)
                        .BorderColor(Colors.Yellow.Lighten2)
                        .Column(notesColumn =>
                        {
                            if (_isInventoryReport)
                            {
                                notesColumn.Item()
                                    .Text("• This report includes all inventory items matching the specified filters.")
                                    .FontSize(9);

                                notesColumn.Item()
                                    .PaddingTop(3)
                                    .Text("• Expired items are highlighted in red.")
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.DangerColor);

                                notesColumn.Item()
                                    .PaddingTop(3)
                                    .Text("• Low stock items (below minimum threshold) are highlighted in orange.")
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.WarningColor);

                                notesColumn.Item()
                                    .PaddingTop(3)
                                    .Text("• Items expiring within 30 days are highlighted in orange.")
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.WarningColor);
                            }
                            else
                            {
                                notesColumn.Item()
                                    .Text("• This report includes all prescriptions matching the specified filters.")
                                    .FontSize(9);

                                notesColumn.Item()
                                    .PaddingTop(3)
                                    .Text("• Pending prescriptions are highlighted in orange.")
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.WarningColor);

                                notesColumn.Item()
                                    .PaddingTop(3)
                                    .Text("• Dispensed prescriptions are highlighted in green.")
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.SuccessColor);

                                notesColumn.Item()
                                    .PaddingTop(3)
                                    .Text("• Cancelled prescriptions are highlighted in red.")
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.DangerColor);
                            }

                            notesColumn.Item()
                                .PaddingTop(3)
                                .Text("• Generated for internal review and reporting purposes.")
                                .FontSize(9);
                        });

                    // Space for handwritten notes
                    column.Item()
                        .PaddingTop(15)
                        .Height(80)
                        .Border(1)
                        .BorderColor(PharmacyPdfTheme.Table.BorderColor)
                        .Background(Colors.White)
                        .Padding(5)
                        .Column(notesColumn =>
                        {
                            notesColumn.Item()
                                .Text("Additional notes:")
                                .FontSize(9)
                                .Italic()
                                .FontColor(Colors.Grey.Medium);
                        });
                });
        }
    }
}
