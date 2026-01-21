using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// Professional PDF table component for inventory items with color coding and styling
    /// </summary>
    public class InventoryTableComponent : IComponent
    {
        private readonly List<Medication> _medications;
        private readonly PdfGenerationSettings _settings;

        public InventoryTableComponent(List<Medication> medications, PdfGenerationSettings settings)
        {
            _medications = medications;
            _settings = settings;
        }

        public void Compose(IContainer container)
        {
            container
                .Column(column =>
                {
                    if (_medications.Count == 0)
                    {
                        column.Item()
                            .PaddingTop(20)
                            .Text("No medications found matching the specified criteria.")
                            .FontSize(12)
                            .Italic()
                            .FontColor(Colors.Grey.Medium);
                        return;
                    }

                    // Table
                    column.Item()
                        .Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);   // Index
                                columns.RelativeColumn(3);    // Name
                                columns.RelativeColumn(2);    // Category
                                columns.RelativeColumn(1.5f); // Price
                                columns.RelativeColumn(1.5f); // Stock
                                columns.RelativeColumn(1.5f); // Status
                                columns.RelativeColumn(2);    // Expiry Date
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("#")
                                    .Bold()
                                    .AlignCenter();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Medication Name")
                                    .Bold();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Category")
                                    .Bold();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Price")
                                    .Bold()
                                    .AlignRight();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Stock")
                                    .Bold()
                                    .AlignRight();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Status")
                                    .Bold()
                                    .AlignCenter();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Expiry Date")
                                    .Bold()
                                    .AlignCenter();

                                // Header separator
                                header.Cell()
                                    .ColumnSpan(7)
                                    .PaddingTop(5)
                                    .LineHorizontal(1)
                                    .LineColor(PharmacyPdfTheme.PrimaryColor);
                            });

                            // Table rows with alternating colors
                            for (int i = 0; i < _medications.Count; i++)
                            {
                                var medication = _medications[i];
                                var rowColor = (i % 2 == 0) ? Colors.White.ToString() : PharmacyPdfTheme.Table.RowAlternateColor;

                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text($"{i + 1}")
                                    .AlignCenter()
                                    .FontSize(9);

                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text(medication.Name ?? "")
                                    .FontSize(9);

                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text(medication.Category ?? "")
                                    .FontSize(9);

                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text($"${medication.Price:F2}")
                                    .AlignRight()
                                    .FontSize(9);

                                // Quantity with color coding
                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text(medication.StockQuantity.ToString())
                                    .AlignRight()
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.GetQuantityColor(
                                        medication.StockQuantity, 
                                        medication.MinimumStockLevel));

                                // Status with color coding
                                var statusText = medication.IsActive ? "Active" : "Inactive";
                                var statusColorString = PharmacyPdfTheme.GetStatusColor(statusText);
                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text(statusText)
                                    .AlignCenter()
                                    .FontSize(9)
                                    .FontColor(statusColorString);

                                // Expiry date with color coding
                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text(medication.ExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A")
                                    .AlignCenter()
                                    .FontSize(9)
                                    .FontColor(PharmacyPdfTheme.GetExpiryColor(medication.ExpiryDate));

                                // Row separator (except for last row)
                                if (i < _medications.Count - 1)
                                {
                                    table.Cell()
                                        .ColumnSpan(7)
                                        .PaddingTop(2)
                                        .LineHorizontal(0.2f)
                                        .LineColor(PharmacyPdfTheme.Table.BorderColor);
                                }
                            }

                            // Table footer with summary
                            table.Footer(footer =>
                            {
                                footer.Cell()
                                    .ColumnSpan(7)
                                    .PaddingTop(10)
                                    .Text($"Total items: {_medications.Count}")
                                    .FontSize(10)
                                    .Italic()
                                    .AlignRight()
                                    .FontColor(PharmacyPdfTheme.SecondaryColor);
                            });
                        });
                });
        }

        private IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(PharmacyPdfTheme.Table.HeaderBackground)
                .Border(1)
                .BorderColor(PharmacyPdfTheme.Table.BorderColor)
                .Padding(8)
                .AlignMiddle();
        }
    }
}
