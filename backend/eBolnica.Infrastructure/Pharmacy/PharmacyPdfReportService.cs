using eBolnica.Application.Abstractions;
using eBolnica.Application.Modules.Pharmacy;
using eBolnica.Application.Modules.Pharmacy.Analytics;
using eBolnica.Domain.Entities.Clinical;
using eBolnica.Domain.Entities.Pharmacy;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnica.Infrastructure.Pharmacy;

public sealed class PharmacyPdfReportService : IPharmacyPdfReportService
{
    private static readonly Color Primary = Color.FromHex("#7C3AED");
    private static readonly Color PageBg = Color.FromHex("#F8FAFC");
    private static readonly Color TextPrimary = Color.FromHex("#111827");
    private static readonly Color TextSecondary = Color.FromHex("#6B7280");
    private static readonly Color Border = Color.FromHex("#E5E7EB");
    private static readonly Color RowAlt = Color.FromHex("#F9FAFB");
    private static readonly Color Success = Color.FromHex("#22C55E");
    private static readonly Color Warning = Color.FromHex("#F59E0B");
    private static readonly Color Danger = Color.FromHex("#EF4444");

    private static readonly Dictionary<string, string> LegacyDosageForms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tablet"] = "Tableta",
        ["Capsule"] = "Kapsula",
        ["Liquid"] = "Tečnost",
        ["Injection"] = "Injekcija",
        ["Cream"] = "Krema",
        ["Drops"] = "Kapi",
        ["Other"] = "Ostalo",
    };

    static PharmacyPdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInventoryPdf(
        InventoryPdfSummary summary,
        IReadOnlyList<MedicationEntity> items)
    {
        var generatedAt = DateTime.UtcNow;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(32);
                page.MarginVertical(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextPrimary));

                page.Background().Background(PageBg);

                page.Header().Element(header =>
                    ComposeInventoryHeader(
                        header,
                        generatedAt,
                        summary.TotalCount,
                        summary.LowStockCount,
                        summary.OutOfStockCount,
                        summary.ExpiringSoonCount));

                page.Content().PaddingTop(12).Element(content =>
                    ComposeInventoryTable(content, items));

                page.Footer().Element(footer => ComposeFooter(footer, generatedAt));
            });
        }).GeneratePdf();
    }

    public byte[] GeneratePrescriptionsPdf(
        PrescriptionsPdfSummary summary,
        IReadOnlyList<PrescriptionEntity> items)
    {
        var generatedAt = DateTime.UtcNow;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(32);
                page.MarginVertical(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextPrimary));

                page.Background().Background(PageBg);

                page.Header().Element(header =>
                    ComposePrescriptionsHeader(header, generatedAt, summary.TotalCount));

                page.Content().PaddingTop(12).Element(content =>
                    ComposePrescriptionsTable(content, items));

                page.Footer().Element(footer => ComposeFooter(footer, generatedAt));
            });
        }).GeneratePdf();
    }

    private static void ComposeInventoryHeader(
        IContainer container,
        DateTime generatedAt,
        int totalCount,
        int lowStockCount,
        int outOfStockCount,
        int expiringSoonCount)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            column.Item().Height(4).Background(Primary);

            column.Item().Background(Colors.White).Border(1).BorderColor(Border).Padding(16).Column(header =>
            {
                header.Item().Row(row =>
                {
                    row.RelativeItem().Column(title =>
                    {
                        title.Item().Text("eBolnica Apoteka").FontSize(18).Bold().FontColor(Primary);
                        title.Item().Text("Inventar lijekova").FontSize(13).SemiBold().FontColor(TextPrimary);
                        title.Item().PaddingTop(4).Text($"Izvještaj generisan: {generatedAt:dd.MM.yyyy HH:mm} UTC")
                            .FontSize(8).FontColor(TextSecondary);
                    });

                    row.ConstantItem(110).AlignRight().Column(meta =>
                    {
                        meta.Item().Background(PageBg).Border(1).BorderColor(Border).Padding(8).Column(box =>
                        {
                            box.Item().Text("Ukupno stavki").FontSize(7).FontColor(TextSecondary);
                            box.Item().Text(totalCount.ToString()).FontSize(16).Bold().FontColor(TextPrimary);
                        });
                    });
                });

                header.Item().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem().Element(c => RenderKpiCard(c, "Ukupno lijekova", totalCount.ToString(), Primary));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => RenderKpiCard(c, "Niska zaliha", lowStockCount.ToString(), Warning));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => RenderKpiCard(c, "Nedostupno", outOfStockCount.ToString(), Danger));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => RenderKpiCard(c, "Ističe uskoro", expiringSoonCount.ToString(), Success));
                });
            });
        });
    }

    private static void ComposePrescriptionsHeader(IContainer container, DateTime generatedAt, int totalCount)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            column.Item().Height(4).Background(Primary);

            column.Item().Background(Colors.White).Border(1).BorderColor(Border).Padding(16).Row(row =>
            {
                row.RelativeItem().Column(title =>
                {
                    title.Item().Text("eBolnica Apoteka").FontSize(18).Bold().FontColor(Primary);
                    title.Item().Text("Izvještaj recepata").FontSize(13).SemiBold().FontColor(TextPrimary);
                    title.Item().PaddingTop(4).Text($"Izvještaj generisan: {generatedAt:dd.MM.yyyy HH:mm} UTC")
                        .FontSize(8).FontColor(TextSecondary);
                });

                row.ConstantItem(110).AlignRight().Element(c =>
                    RenderKpiCard(c, "Ukupno recepata", totalCount.ToString(), Primary));
            });
        });
    }

    private static void RenderKpiCard(IContainer container, string label, string value, Color accent)
    {
        container.Background(Colors.White).Border(1).BorderColor(Border).Padding(10).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(label).FontSize(7).FontColor(TextSecondary);
                row.ConstantItem(8).Height(8).Width(8).Background(accent);
            });
            column.Item().PaddingTop(4).Text(value).FontSize(14).Bold().FontColor(TextPrimary);
        });
    }

    private static void ComposeInventoryTable(
        IContainer container,
        IReadOnlyList<MedicationEntity> items)
    {
        container.Background(Colors.White).Border(1).BorderColor(Border).Padding(12).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(22);
                columns.RelativeColumn(3.2f);
                columns.RelativeColumn(1.8f);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(1.3f);
            });

            table.Header(header =>
            {
                RenderHeaderCell(header.Cell(), "#");
                RenderHeaderCell(header.Cell(), "Naziv");
                RenderHeaderCell(header.Cell(), "Kategorija");
                RenderHeaderCell(header.Cell(), "Oblik doze");
                RenderHeaderCell(header.Cell(), "Cijena");
                RenderHeaderCell(header.Cell(), "Zaliha");
                RenderHeaderCell(header.Cell(), "Status");
                RenderHeaderCell(header.Cell(), "Rok");
            });

            for (var rowIndex = 0; rowIndex < items.Count; rowIndex++)
            {
                var medication = items[rowIndex];
                var rowBg = rowIndex % 2 == 0 ? Colors.White : RowAlt;
                var status = GetStockStatus(medication);

                RenderBodyCell(table.Cell(), rowBg, (rowIndex + 1).ToString(), TextSecondary);
                RenderNameCell(table.Cell(), rowBg, medication);
                RenderBodyCell(table.Cell(), rowBg, medication.Category ?? "-");
                RenderBodyCell(table.Cell(), rowBg, FormatDosageForm(medication.DosageForm));
                RenderBodyCell(table.Cell(), rowBg, $"{medication.Price:F2} KM", TextPrimary, true);
                RenderBodyCell(table.Cell(), rowBg, medication.StockQuantity.ToString(), TextPrimary, true);
                RenderStatusCell(table.Cell(), rowBg, status);
                RenderBodyCell(table.Cell(), rowBg, medication.ExpiryDate?.ToString("dd.MM.yyyy") ?? "-");
            }
        });
    }

    private static void ComposePrescriptionsTable(
        IContainer container,
        IReadOnlyList<PrescriptionEntity> items)
    {
        container.Background(Colors.White).Border(1).BorderColor(Border).Padding(12).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(22);
                columns.RelativeColumn(1.6f);
                columns.RelativeColumn(2.2f);
                columns.RelativeColumn(2.2f);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(1.5f);
            });

            table.Header(header =>
            {
                RenderHeaderCell(header.Cell(), "#");
                RenderHeaderCell(header.Cell(), "Broj");
                RenderHeaderCell(header.Cell(), "Pacijent");
                RenderHeaderCell(header.Cell(), "Doktor");
                RenderHeaderCell(header.Cell(), "Status");
                RenderHeaderCell(header.Cell(), "Iznos");
                RenderHeaderCell(header.Cell(), "Datum");
            });

            for (var rowIndex = 0; rowIndex < items.Count; rowIndex++)
            {
                var prescription = items[rowIndex];
                var rowBg = rowIndex % 2 == 0 ? Colors.White : RowAlt;

                RenderBodyCell(table.Cell(), rowBg, (rowIndex + 1).ToString(), TextSecondary);
                RenderBodyCell(table.Cell(), rowBg, prescription.PrescriptionNumber, TextPrimary, true);
                RenderBodyCell(table.Cell(), rowBg, FormatPatientName(prescription.Patient));
                RenderBodyCell(table.Cell(), rowBg, FormatDoctorName(prescription.Doctor));
                RenderBodyCell(table.Cell(), rowBg, prescription.Status);
                RenderBodyCell(table.Cell(), rowBg, $"{prescription.TotalAmount:F2} KM", TextPrimary, true);
                RenderBodyCell(table.Cell(), rowBg, prescription.PrescribedDate.ToString("dd.MM.yyyy"));
            }
        });
    }

    private static void ComposeFooter(IContainer container, DateTime generatedAt)
    {
        container.PaddingTop(8).Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(TextSecondary));
                text.Span("eBolnica · Apoteka · ");
                text.Span(generatedAt.ToString("dd.MM.yyyy HH:mm"));
            });

            row.RelativeItem().AlignRight().DefaultTextStyle(x => x.FontSize(8).FontColor(TextSecondary)).Text(text =>
            {
                text.Span("Strana ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static void RenderHeaderCell(IContainer cell, string label) =>
        cell.Background(TextPrimary).BorderBottom(1).BorderColor(Border).PaddingVertical(8).PaddingHorizontal(6)
            .Text(label.ToUpperInvariant()).FontSize(7).SemiBold().FontColor(Colors.White).LetterSpacing(0.4f);

    private static void RenderBodyCell(
        IContainer cell,
        Color rowBg,
        string value,
        Color? color = null,
        bool semiBold = false)
    {
        var text = cell.Background(rowBg).BorderBottom(1).BorderColor(Border).PaddingVertical(7).PaddingHorizontal(6).Text(value);
        text.FontSize(8).FontColor(color ?? TextPrimary);
        if (semiBold)
            text.SemiBold();
    }

    private static void RenderNameCell(IContainer cell, Color rowBg, MedicationEntity medication)
    {
        cell.Background(rowBg).BorderBottom(1).BorderColor(Border).PaddingVertical(7).PaddingHorizontal(6).Column(column =>
        {
            column.Item().Text(medication.Name).FontSize(8).SemiBold().FontColor(TextPrimary);
            if (!string.IsNullOrWhiteSpace(medication.GenericName))
                column.Item().Text(medication.GenericName).FontSize(7).FontColor(TextSecondary);
        });
    }

    private static void RenderStatusCell(IContainer cell, Color rowBg, StockStatus status)
    {
        var (label, color) = status switch
        {
            StockStatus.Available => ("Dostupno", Success),
            StockStatus.Low => ("Niska zaliha", Warning),
            StockStatus.Out => ("Nedostupno", Danger),
            _ => ("Neaktivan", TextSecondary),
        };

        cell.Background(rowBg).BorderBottom(1).BorderColor(Border).PaddingVertical(7).PaddingHorizontal(6).AlignMiddle()
            .Element(container =>
            {
                container.Background(color).CornerRadius(10).PaddingVertical(3).PaddingHorizontal(8).Text(label)
                    .FontSize(7).SemiBold().FontColor(Colors.White);
            });
    }

    private static StockStatus GetStockStatus(MedicationEntity medication)
    {
        if (!medication.IsActive)
            return StockStatus.Inactive;

        if (medication.StockQuantity <= 0)
            return StockStatus.Out;

        if (medication.StockQuantity < medication.MinimumStockLevel)
            return StockStatus.Low;

        return StockStatus.Available;
    }

    private static string FormatDosageForm(string? dosageForm)
    {
        if (string.IsNullOrWhiteSpace(dosageForm))
            return "-";

        return LegacyDosageForms.TryGetValue(dosageForm.Trim(), out var translated)
            ? translated
            : dosageForm.Trim();
    }

    private static string FormatPatientName(PatientEntity? patient) =>
        patient is null ? "-" : $"{patient.FirstName} {patient.LastName}";

    private static string FormatDoctorName(DoctorEntity? doctor) =>
        doctor is null ? "-" : $"Dr. {doctor.FirstName} {doctor.LastName}";

    private enum StockStatus
    {
        Available,
        Low,
        Out,
        Inactive,
    }
}
