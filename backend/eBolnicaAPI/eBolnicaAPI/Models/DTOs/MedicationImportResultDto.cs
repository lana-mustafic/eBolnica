namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Result of a medication CSV batch import operation.
    /// </summary>
    public class MedicationImportResultDto
    {
        /// <summary>Number of rows persisted successfully.</summary>
        public int SuccessCount { get; set; }

        /// <summary>Number of rows rejected during validation or duplicate checks.</summary>
        public int FailureCount { get; set; }

        /// <summary>Total data rows processed (excluding header).</summary>
        public int TotalRows { get; set; }

        /// <summary>Whether the validated batch was committed to the database.</summary>
        public bool Committed { get; set; }

        /// <summary>Database ids of medications created in this import batch.</summary>
        public List<int> ImportedMedicationIds { get; set; } = new();

        /// <summary>Per-row validation or business rule failures.</summary>
        public List<MedicationImportRowErrorDto> Errors { get; set; } = new();

        /// <summary>Present when validation passed but the batch transaction failed.</summary>
        public string? BatchError { get; set; }
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
