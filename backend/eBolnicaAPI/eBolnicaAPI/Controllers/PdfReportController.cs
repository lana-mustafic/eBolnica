using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eBolnicaAPI.Controllers
{
    [Route("api/patient/medical-record")]
    [ApiController]
    public class PdfReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PdfReportController(AppDbContext context) {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        [HttpGet("pdf/{medicalRecordId}")]
        public async Task<IActionResult> GenerateMedicalRecordPdf(int medicalRecordId, [FromQuery] DateTime dateFrom, [FromQuery] DateTime dateTo)
        {
            var medicalRecord = await _context.MedicalRecords.Include(mr => mr.Patient)
                .Include(mr => mr.MedicalReports.Where(rep => rep.CreatedAt >= dateFrom && rep.CreatedAt <= dateTo))
                .ThenInclude(rep => rep.Doctor).FirstOrDefaultAsync(mr => mr.Id == medicalRecordId);

            if(medicalRecord == null)
            {
                return NotFound(new { message = "Medical Record not found" });
            }

            var document = CreateMedicalRecordDocument(medicalRecord, dateFrom, dateTo);
            var pdfBytes = document.GeneratePdf();

            var fileName = $"MedicalRecord_{medicalRecord.RecordNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
            
        }


        private Document CreateMedicalRecordDocument(MedicalRecord record, DateTime dateFrom, DateTime dateTo) {

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(header => ComposeHeader(header, record));

                    page.Content().Element(content => ComposeContent(content, record, dateFrom, dateTo));
                });
            });
        }


        void ComposeHeader(IContainer container, MedicalRecord record)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item()
                            .Text("MEDICAL REPORT REPORT")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                    });

                    row.ConstantItem(120).AlignRight().Column(col =>
                    {
                        col.Item()
                            .Text($"Record #: {record.RecordNumber}")
                            .Bold()
                            .FontSize(11)
                            .FontColor(Colors.Blue.Darken2);

                        col.Item()
                            .Text($"Patient ID #: {record.PatientId}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                column.Item()
                    .PaddingTop(10)
                    .LineHorizontal(2)
                    .LineColor(Colors.Blue.Darken2);
            });
        }


        void ComposeContent(IContainer container, MedicalRecord record, DateTime dateFrom, DateTime dateTo)
        {
            var reports = record.MedicalReports?.ToList() ?? new List<MedicalReport>();

            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(15);

                column.Item().Element(c => ComposeFilterInfo(c, dateFrom, dateTo));
                column.Item().Element(c => ComposePatientInfo(c, record.Patient));
                column.Item().Element(c => ComposeSummary(c, reports));
                column.Item().Element(c => ComposeMedicalReports(c, reports));
            });
        }
        void ComposeFilterInfo(IContainer container, DateTime dateFrom, DateTime dateTo)
        {
            container.Background(Colors.Yellow.Lighten3).Padding(12).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Report period: ").Bold().FontSize(11);
                    text.Span($"{dateFrom:dd.MM.yyyy} - {dateTo:dd.MM.yyyy}").FontSize(11);
                });

                row.ConstantItem(150).AlignRight().Text(text =>
                {
                    text.Span("Generated: ").FontSize(10).FontColor(Colors.Grey.Darken1);
                    text.Span(DateTime.Now.ToString("dd.MMMM.yyyy HH:mm")).Bold().FontSize(10);
                });
            });
        }

        void ComposePatientInfo(IContainer container, Patient patient)
        {
            container.Background(Colors.Blue.Lighten4).Padding(15).Column(column =>
            {
                column.Item().Text("PATIENT INFORMATION")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(10).Row(row =>
                {
  
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Full Name:").FontSize(10).Bold();
                            r.RelativeItem().Text($"{patient.FirstName} {patient.LastName}").FontSize(10);
                        });

                        col.Item().PaddingTop(5).Row(r =>
                        {
                            r.ConstantItem(120).Text("Date of Birth:").FontSize(10).Bold();
                            r.RelativeItem().Text(patient.DateOfBirth?.ToString("dd.MM.yyyy") ?? "N/A").FontSize(10);
                        });

                        col.Item().PaddingTop(5).Row(r =>
                        {
                            r.ConstantItem(120).Text("Gender:").FontSize(10).Bold();
                            r.RelativeItem().Text(patient.Gender ?? "N/A").FontSize(10);
                        });

                        col.Item().PaddingTop(5).Row(r =>
                        {
                            r.ConstantItem(120).Text("Blood Type:").FontSize(10).Bold();
                            r.RelativeItem().Text(patient.BloodType ?? "N/A").FontSize(10).FontColor(Colors.Red.Darken1);
                        });
                    });


                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(100).Text("Phone:").FontSize(10).Bold();
                            r.RelativeItem().Text(patient.PhoneNumber ?? "N/A").FontSize(10);
                        });

                        col.Item().PaddingTop(5).Row(r =>
                        {
                            r.ConstantItem(100).Text("Address:").FontSize(10).Bold();
                            r.RelativeItem().Text(patient.Address ?? "N/A").FontSize(10);
                        });

                        col.Item().PaddingTop(5).Row(r =>
                        {
                            r.ConstantItem(100).Text("Admitted:").FontSize(10).Bold();
                            r.RelativeItem().Text(patient.IsAdmitted == true ? "Yes ✓" : "No").FontSize(10)
                                .FontColor(patient.IsAdmitted == true ? Colors.Green.Darken1 : Colors.Grey.Darken1);
                        });
                    });
                });
            });
        }

        void ComposeSummary(IContainer container, List<MedicalReport> reports)
        {
            container.Background(Colors.Grey.Lighten3).Padding(12).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Total Medical Reports in Period: ").FontSize(11);
                    text.Span(reports.Count.ToString()).Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                });
            });
        }

        void ComposeMedicalReports(IContainer container, List<MedicalReport> reports)
        {
            container.Column(column =>
            {
                column.Item().Text("MEDICAL REPORTS")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5);

                if (!reports.Any())
                {
                    column.Item().Background(Colors.Yellow.Lighten4).Padding(15).Text(
                        "No medical reports found in the selected period.")
                        .FontSize(11).Italic().FontColor(Colors.Orange.Darken1);
                    return;
                }

                var sortedReports = reports.OrderByDescending(r => r.CreatedAt).ToList();

                foreach (var report in sortedReports)
                {
                    column.Item().PaddingTop(12).Element(c => ComposeSingleReport(c, report));
                }
            });
        }

        void ComposeSingleReport(IContainer container, MedicalReport report)
        {
            container.Border(1.5f).BorderColor(Colors.Blue.Lighten1).Padding(15).Column(column =>
            {
                column.Item().Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("").FontSize(12);
                        text.Span(report.CreatedAt.ToString("dd.MM.yyyy HH:mm")).Bold().FontSize(11);
                    });

                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Dr. ").FontSize(10);
                        text.Span(report.Doctor != null
                            ? $"{report.Doctor.FirstName} {report.Doctor.LastName}"
                            : "Unknown").Bold().FontSize(11);
                    });
                });

                column.Item().PaddingTop(10);

                if (!string.IsNullOrEmpty(report.Symptoms))
                {
                    column.Item().Column(col =>
                    {
                        col.Item().Text("Symptoms:").FontSize(10).Bold().FontColor(Colors.Orange.Darken1);
                        col.Item().PaddingLeft(15).PaddingTop(3).Text(report.Symptoms).FontSize(10);
                    });
                    column.Item().PaddingTop(8);
                }

                if (!string.IsNullOrEmpty(report.Diagnosis))
                {
                    column.Item().Column(col =>
                    {
                        col.Item().Text("Diagnosis:").FontSize(10).Bold().FontColor(Colors.Red.Darken1);
                        col.Item().PaddingLeft(15).PaddingTop(3).Text(report.Diagnosis).FontSize(10);
                    });
                    column.Item().PaddingTop(8);
                }

                if (!string.IsNullOrEmpty(report.Therapy))
                {
                    column.Item().Column(col =>
                    {
                        col.Item().Text("Prescribed Therapy:").FontSize(10).Bold().FontColor(Colors.Green.Darken1);
                        col.Item().PaddingLeft(15).PaddingTop(3).Text(report.Therapy).FontSize(10);
                    });
                    column.Item().PaddingTop(8);
                }

                if (!string.IsNullOrEmpty(report.Description))
                {
                    column.Item().Column(col =>
                    {
                        col.Item().Text("Additional Notes:").FontSize(10).Bold().FontColor(Colors.Blue.Darken1);
                        col.Item().PaddingLeft(15).PaddingTop(3).Background(Colors.Grey.Lighten4)
                            .Padding(8).Text(report.Description).FontSize(9).Italic();
                    });
                }

                column.Item().PaddingTop(8).AlignRight().Text($"Report ID: #{report.Id}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        }
    }
} 

