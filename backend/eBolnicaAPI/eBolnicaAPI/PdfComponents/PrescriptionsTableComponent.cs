using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Models.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.PdfComponents
{
    /// <summary>
    /// Professional PDF table component for prescriptions with color coding and styling
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
                                columns.ConstantColumn(35);   // Index
                                columns.RelativeColumn(2);    // Prescription #
                                columns.RelativeColumn(2.5f); // Patient
                                columns.RelativeColumn(2);    // Doctor
                                columns.RelativeColumn(1.5f); // Status
                                columns.RelativeColumn(1.5f); // Total Amount
                                columns.RelativeColumn(1.5f); // Date
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
                                    .Text("Prescription #")
                                    .Bold();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Patient")
                                    .Bold();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Doctor")
                                    .Bold();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Status")
                                    .Bold()
                                    .AlignCenter();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Total Amount")
                                    .Bold()
                                    .AlignRight();

                                header.Cell()
                                    .Element(HeaderCellStyle)
                                    .Text("Date")
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
                            for (int i = 0; i < _prescriptions.Count; i++)
                            {
                                var prescription = _prescriptions[i];
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
                                    .Text(prescription.PrescriptionNumber ?? "")
                                    .FontSize(9);

                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text($"{prescription.Patient?.FirstName ?? ""} {prescription.Patient?.LastName ?? ""}".Trim())
                                    .FontSize(9);

                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text($"{prescription.Doctor?.FirstName ?? ""} {prescription.Doctor?.LastName ?? ""}".Trim())
                                    .FontSize(9);

                                // Status with color coding
                                var statusColorString = PharmacyPdfTheme.GetStatusColor(prescription.Status ?? "");
                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text(prescription.Status ?? "")
                                    .AlignCenter()
                                    .FontSize(9)
                                    .FontColor(statusColorString);

                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text($"${prescription.TotalAmount:F2}")
                                    .AlignRight()
                                    .FontSize(9);

                                var prescriptionDateText = prescription.PrescribedDate != default(DateTime)
                                    ? prescription.PrescribedDate.ToString("yyyy-MM-dd")
                                    : "N/A";
                                table.Cell()
                                    .Background(rowColor)
                                    .PaddingVertical(6)
                                    .Text(prescriptionDateText)
                                    .AlignCenter()
                                    .FontSize(9);

                                // Row separator (except for last row)
                                if (i < _prescriptions.Count - 1)
                                {
                                    table.Cell()
                                        .ColumnSpan(7)
                                        .PaddingTop(2)
                                        .LineHorizontal(0.2f)
                                        .LineColor(PharmacyPdfTheme.Table.BorderColor);
                                }
                            }

                            // Table footer with summary
                            var totalAmount = _prescriptions.Sum(p => p.TotalAmount);
                            table.Footer(footer =>
                            {
                                footer.Cell()
                                    .ColumnSpan(5)
                                    .PaddingTop(10)
                                    .Text($"Total: {_prescriptions.Count} prescriptions")
                                    .FontSize(10)
                                    .Italic()
                                    .FontColor(PharmacyPdfTheme.SecondaryColor);

                                footer.Cell()
                                    .ColumnSpan(2)
                                    .PaddingTop(10)
                                    .Text($"Total Amount: ${totalAmount:F2}")
                                    .FontSize(10)
                                    .Bold()
                                    .AlignRight()
                                    .FontColor(PharmacyPdfTheme.PrimaryColor);
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
