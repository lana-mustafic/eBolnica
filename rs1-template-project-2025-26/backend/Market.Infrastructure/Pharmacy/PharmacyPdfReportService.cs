using Market.Application.Abstractions;
using Market.Application.Modules.Pharmacy.Analytics;
using Market.Domain.Entities.Pharmacy;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Market.Infrastructure.Pharmacy;

public sealed class PharmacyPdfReportService : IPharmacyPdfReportService
{
    static PharmacyPdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInventoryPdf(IReadOnlyList<MedicationEntity> medications) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Text("eBolnica — Inventar lijekova").Bold().FontSize(18);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(24);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("#").Bold();
                        h.Cell().Text("Naziv").Bold();
                        h.Cell().Text("Kategorija").Bold();
                        h.Cell().Text("Cijena").Bold();
                        h.Cell().Text("Zaliha").Bold();
                        h.Cell().Text("Rok").Bold();
                    });
                    for (var i = 0; i < medications.Count; i++)
                    {
                        var m = medications[i];
                        table.Cell().Text((i + 1).ToString());
                        table.Cell().Text(m.Name);
                        table.Cell().Text(m.Category ?? "-");
                        table.Cell().Text($"{m.Price:F2} KM");
                        table.Cell().Text(m.StockQuantity.ToString());
                        table.Cell().Text(m.ExpiryDate?.ToString("dd.MM.yyyy") ?? "-");
                    }
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generisano: ");
                    t.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                });
            });
        }).GeneratePdf();

    public byte[] GeneratePrescriptionsPdf(IReadOnlyList<PrescriptionEntity> prescriptions) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Text("eBolnica — Izvještaj recepata").Bold().FontSize(18);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(24);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("#").Bold();
                        h.Cell().Text("Broj").Bold();
                        h.Cell().Text("Pacijent").Bold();
                        h.Cell().Text("Doktor").Bold();
                        h.Cell().Text("Status").Bold();
                        h.Cell().Text("Iznos").Bold();
                        h.Cell().Text("Datum").Bold();
                    });
                    for (var i = 0; i < prescriptions.Count; i++)
                    {
                        var p = prescriptions[i];
                        table.Cell().Text((i + 1).ToString());
                        table.Cell().Text(p.PrescriptionNumber);
                        table.Cell().Text($"{p.Patient.FirstName} {p.Patient.LastName}");
                        table.Cell().Text($"Dr. {p.Doctor.FirstName} {p.Doctor.LastName}");
                        table.Cell().Text(p.Status);
                        table.Cell().Text($"{p.TotalAmount:F2} KM");
                        table.Cell().Text(p.PrescribedDate.ToString("dd.MM.yyyy"));
                    }
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generisano: ");
                    t.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                });
            });
        }).GeneratePdf();
}
