namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Result of a medication CSV import operation.
    /// </summary>
    public class MedicationImportSummaryDto
    {
        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public int TotalRows { get; set; }

        public List<MedicationImportRowErrorDto> Errors { get; set; } = new();
    }

    /// <summary>
    /// A single failed import row.
    /// </summary>
    public class MedicationImportRowErrorDto
    {
        /// <summary>1-based CSV row number (header is row 1).</summary>
        public int RowNumber { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Field { get; set; }

        public string? Value { get; set; }
    }
}
