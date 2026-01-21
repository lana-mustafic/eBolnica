using eBolnicaAPI.Models;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// Professional pharmacy header component with logo and contact information
    /// </summary>
    public class PharmacyHeaderComponent : IComponent
    {
        private readonly PharmacyInfo _pharmacyInfo;
        private readonly PdfGenerationSettings _settings;

        public PharmacyHeaderComponent(PharmacyInfo pharmacyInfo, PdfGenerationSettings settings)
        {
            _pharmacyInfo = pharmacyInfo;
            _settings = settings;
        }

        public void Compose(IContainer container)
        {
            container
                .Column(column =>
                {
                    // Header row
                    column.Item()
                        .Row(row =>
                        {
                            // Left side: Pharmacy info
                            row.RelativeItem(2).Column(pharmacyColumn =>
                            {
                                pharmacyColumn.Item()
                                    .Text(_pharmacyInfo.Name)
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(PharmacyPdfTheme.PrimaryColor);

                                pharmacyColumn.Item()
                                    .PaddingTop(2)
                                    .Text(_pharmacyInfo.Address)
                                    .FontSize(10)
                                    .FontColor(PharmacyPdfTheme.SecondaryColor);

                                pharmacyColumn.Item()
                                    .PaddingTop(2)
                                    .Text($"Phone: {_pharmacyInfo.Phone} | Email: {_pharmacyInfo.Email}")
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.SecondaryColor);

                                if (!string.IsNullOrEmpty(_pharmacyInfo.LicenseNumber))
                                {
                                    pharmacyColumn.Item()
                                        .PaddingTop(2)
                                        .Text($"License: {_pharmacyInfo.LicenseNumber}")
                                        .FontSize(9)
                                        .FontColor(PharmacyPdfTheme.SecondaryColor);
                                }
                            });

                            // Right side: Logo placeholder
                            row.RelativeItem(1).AlignRight().Column(logoColumn =>
                            {
                                logoColumn.Item()
                                    .Width(60)
                                    .Height(60)
                                    .Background(PharmacyPdfTheme.PrimaryColor)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text("PH")
                                    .FontColor(Colors.White)
                                    .FontSize(24)
                                    .Bold();

                                logoColumn.Item()
                                    .PaddingTop(2)
                                    .Text("Pharmacy")
                                    .FontSize(10)
                                    .AlignCenter()
                                    .FontColor(PharmacyPdfTheme.SecondaryColor);
                            });
                        });

                    // Separator line
                    column.Item()
                        .PaddingTop(10)
                        .LineHorizontal(1)
                        .LineColor(PharmacyPdfTheme.PrimaryColor);
                });
        }
    }
}
