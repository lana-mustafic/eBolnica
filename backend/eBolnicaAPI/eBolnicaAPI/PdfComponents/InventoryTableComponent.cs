using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// PDF table component for inventory items
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
                    // Summary section
                    column.Item()
                        .PaddingBottom(10)
                        .Text($"Showing {_medications.Count} medication(s)")
                        .FontSize(11)
                        .Bold();

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
                                columns.RelativeColumn(3); // Name
                                columns.RelativeColumn(2); // Category
                                columns.RelativeColumn(2); // Price
                                columns.RelativeColumn(2); // Stock
                                columns.RelativeColumn(2); // Status
                                columns.RelativeColumn(2); // Expiry Date
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Name");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Category");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Price");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Stock");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Status");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Expiry Date");
                            });

                            // Table rows
                            foreach (var medication in _medications)
                            {
                                table.Cell()
                                    .Element(CellStyle)
                                    .Text(medication.Name ?? "");

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text(medication.Category ?? "");

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text($"${medication.Price:F2}");

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text(medication.StockQuantity.ToString());

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text(medication.IsActive ? "Active" : "Inactive");

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text(medication.ExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A");
                            }
                        });
                });
        }

        private IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .Padding(5)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten1)
                .AlignCenter()
                .AlignMiddle();
        }

        private IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5)
                .AlignMiddle();
        }
    }
}
