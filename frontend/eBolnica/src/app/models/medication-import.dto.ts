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
  /** Per-row validation or business rule failures. */
  errors: MedicationImportRowError[];
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
