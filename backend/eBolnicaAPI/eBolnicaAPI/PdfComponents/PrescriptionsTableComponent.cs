using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// PDF table component for prescriptions
    /// </summary>
    public class PrescriptionsTableComponent : IComponent
    {
        private readonly List<Prescription> _prescriptions;
        private readonly PdfGenerationSettings _settings;

        public PrescriptionsTableComponent(List<Prescription> prescriptions, PdfGenerationSettings settings)
        {
            _prescriptions = prescriptions;
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
                        .Text($"Showing {_prescriptions.Count} prescription(s)")
                        .FontSize(11)
                        .Bold();

                    if (_prescriptions.Count == 0)
                    {
                        column.Item()
                            .PaddingTop(20)
                            .Text("No prescriptions found matching the specified criteria.")
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
                                columns.RelativeColumn(2); // Prescription #
                                columns.RelativeColumn(2); // Patient
                                columns.RelativeColumn(2); // Doctor
                                columns.RelativeColumn(2); // Status
                                columns.RelativeColumn(2); // Total Amount
                                columns.RelativeColumn(2); // Date
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Prescription #");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Patient");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Doctor");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Status");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Total Amount");
                                
                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Date");
                            });

                            // Table rows
                            foreach (var prescription in _prescriptions)
                            {
                                table.Cell()
                                    .Element(CellStyle)
                                    .Text(prescription.PrescriptionNumber ?? "");

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text($"{prescription.Patient?.FirstName ?? ""} {prescription.Patient?.LastName ?? ""}".Trim());

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text($"{prescription.Doctor?.FirstName ?? ""} {prescription.Doctor?.LastName ?? ""}".Trim());

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text(prescription.Status ?? "");

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text($"${prescription.TotalAmount:F2}");

                                table.Cell()
                                    .Element(CellStyle)
                                    .Text(prescription.PrescribedDate?.ToString("yyyy-MM-dd") ?? "N/A");
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
