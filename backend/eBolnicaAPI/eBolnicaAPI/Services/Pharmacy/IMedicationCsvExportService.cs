using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Builds medication list CSV exports aligned with the pharmacy import/export template.
    /// </summary>
    public interface IMedicationCsvExportService
    {
        /// <summary>Maximum number of rows allowed in a single export response.</summary>
        int MaxExportRows { get; }

        string BuildCsv(IEnumerable<Medication> medications);

        string GetExportFileName(DateTime? timestamp = null);

        string BuildImportTemplateCsv();

        string GetImportTemplateFileName();
    }
}
