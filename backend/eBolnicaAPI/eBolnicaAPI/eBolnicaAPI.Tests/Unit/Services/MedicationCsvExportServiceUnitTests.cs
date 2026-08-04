using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy;
using System;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationCsvExportServiceUnitTests
    {
        private readonly MedicationCsvExportService _service = new();

        [Fact]
        public void BuildCsv_IncludesHeadersAndStatusColumn()
        {
            var csv = _service.BuildCsv(new[]
            {
                new Medication
                {
                    Name = "Paracetamol",
                    Price = 9.99m,
                    StockQuantity = 10,
                    MinimumStockLevel = 5,
                    IsActive = true,
                    RequiresPrescription = false,
                    Category = "Painkillers",
                    CreatedAt = DateTime.UtcNow
                }
            });

            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Contains("Status", lines[0]);
            Assert.Contains("Paracetamol", lines[1]);
            Assert.Contains("Active", lines[1]);
        }

        [Fact]
        public void BuildCsv_EscapesCommasInDescription()
        {
            var csv = _service.BuildCsv(new[]
            {
                new Medication
                {
                    Name = "Test Med",
                    Description = "Pain, fever",
                    Price = 1m,
                    StockQuantity = 1,
                    MinimumStockLevel = 1,
                    IsActive = true,
                    RequiresPrescription = false,
                    CreatedAt = DateTime.UtcNow
                }
            });

            Assert.Contains("\"Pain, fever\"", csv);
        }

        [Fact]
        public void GetExportFileName_UsesUtcDatePrefix()
        {
            var fileName = _service.GetExportFileName(new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc));
            Assert.Equal("pharmacy-medications-2026-03-15.csv", fileName);
        }

        [Fact]
        public void BuildImportTemplateCsv_IncludesHeadersAndExampleRowWithoutStatus()
        {
            var csv = _service.BuildImportTemplateCsv();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            Assert.Equal(2, lines.Length);
            Assert.Contains("Requires Prescription", lines[0]);
            Assert.DoesNotContain("Status", lines[0]);
            Assert.Contains("Paracetamol (required, 3-100 characters)", lines[1]);
            Assert.Contains("2026-12-31 (required, YYYY-MM-DD, must be future date)", lines[1]);
        }

        [Fact]
        public void GetImportTemplateFileName_ReturnsFixedName()
        {
            Assert.Equal("medication-import-template.csv", _service.GetImportTemplateFileName());
        }
    }
}
