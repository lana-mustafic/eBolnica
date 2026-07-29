/**
 * Result of a medication CSV import operation.
 */
export interface MedicationImportSummary {
  /** Number of rows imported successfully. */
  successCount: number;
  /** Number of rows rejected. */
  failureCount: number;
  /** Total data rows processed (excluding header). */
  totalRows: number;
  /** Whether the validated batch was committed to the database. */
  committed: boolean;
  /** Database ids of medications created in this import batch. */
  importedMedicationIds: number[];
  /** Per-row validation or business rule failures. */
  errors: MedicationImportRowError[];
  /** Present when validation passed but the batch transaction failed. */
  batchError?: string;
}

/**
 * A single failed import row.
 */
export interface MedicationImportRowError {
  /** 1-based CSV row number (header is row 1; first data row is 2). */
  rowNumber: number;
  /** Human-readable rejection reason. */
  reason: string;
  /** Optional column/field name when validation is field-specific. */
  field?: string;
  /** Optional cell value that failed validation. */
  value?: string;
}
