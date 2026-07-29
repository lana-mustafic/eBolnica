import { MedicationDto } from '../../models/medication.dto';

/** CSV columns used for medication import (export adds Status). */
export const MEDICATION_IMPORT_CSV_HEADERS = [
  'Name',
  'Generic Name',
  'Category',
  'Manufacturer',
  'Description',
  'Price',
  'Stock Quantity',
  'Minimum Stock Level',
  'Expiry Date',
  'Batch Number',
  'Dosage Form',
  'Strength',
  'Requires Prescription',
  'Active'
] as const;

export const MEDICATION_EXPORT_CSV_HEADERS = [
  ...MEDICATION_IMPORT_CSV_HEADERS,
  'Status'
] as const;

export const MEDICATION_IMPORT_TEMPLATE_FILENAME = 'medication-import-template.csv';

export const MAX_MEDICATION_IMPORT_FILE_BYTES = 5 * 1024 * 1024;

export const MAX_MEDICATION_IMPORT_FILE_SIZE_LABEL = '5 MB';

const MEDICATION_IMPORT_TEMPLATE_EXAMPLE_ROW = [
  'Paracetamol (required, 3-100 characters)',
  'Acetaminophen (optional)',
  'Painkillers (required)',
  'PharmaCorp (optional)',
  'Pain reliever (optional, max 500 characters)',
  '9.99 (required, > 0)',
  '100 (required, integer >= 0)',
  '20 (required, integer >= 0)',
  '2026-12-31 (required, YYYY-MM-DD, must be future date)',
  'BATCH-001 (optional)',
  'Tablet (optional)',
  '500mg (optional)',
  'No (required: Yes or No)',
  'Yes (required: Yes or No)'
];

export function escapeMedicationCsvValue(value: string): string {
  if (!value) {
    return '';
  }

  if (value.includes(',') || value.includes('"') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`;
  }

  return value;
}

export function formatMedicationDateForCsv(dateString: string | undefined): string {
  if (!dateString) {
    return '';
  }

  const date = new Date(dateString);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return date.toISOString().split('T')[0];
}

export function getMedicationStockStatusLabel(medication: MedicationDto): string {
  if (!medication.isActive) {
    return 'Inactive';
  }

  if (medication.expiryDate) {
    const expiry = new Date(medication.expiryDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (expiry < today) {
      return 'Expired';
    }
  }

  if (medication.stockQuantity === 0) {
    return 'Out of Stock';
  }

  if (medication.stockQuantity < medication.minimumStockLevel) {
    return 'Low Stock';
  }

  return 'Active';
}

export function buildMedicationExportCsv(medications: MedicationDto[]): string {
  const rows = medications.map(medication => [
    escapeMedicationCsvValue(medication.name),
    escapeMedicationCsvValue(medication.genericName || ''),
    escapeMedicationCsvValue(medication.category || ''),
    escapeMedicationCsvValue(medication.manufacturer || ''),
    escapeMedicationCsvValue(medication.description || ''),
    medication.price.toString(),
    medication.stockQuantity.toString(),
    medication.minimumStockLevel.toString(),
    medication.expiryDate ? formatMedicationDateForCsv(medication.expiryDate) : '',
    escapeMedicationCsvValue(medication.batchNumber || ''),
    escapeMedicationCsvValue(medication.dosageForm || ''),
    escapeMedicationCsvValue(medication.strength || ''),
    medication.requiresPrescription ? 'Yes' : 'No',
    medication.isActive ? 'Yes' : 'No',
    escapeMedicationCsvValue(getMedicationStockStatusLabel(medication))
  ]);

  return [MEDICATION_EXPORT_CSV_HEADERS.join(','), ...rows.map(row => row.join(','))].join('\n');
}

export function buildMedicationImportTemplateCsv(): string {
  const exampleRow = MEDICATION_IMPORT_TEMPLATE_EXAMPLE_ROW.map(value => escapeMedicationCsvValue(value));
  return [MEDICATION_IMPORT_CSV_HEADERS.join(','), exampleRow.join(',')].join('\n');
}

export function getMedicationExportFilename(date: Date = new Date()): string {
  const day = date.toISOString().split('T')[0];
  return `pharmacy-medications-${day}.csv`;
}

export function downloadMedicationCsv(content: string, filename: string): void {
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.style.display = 'none';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}

export function isMedicationCsvFile(file: File): boolean {
  const name = file.name.toLowerCase();
  return name.endsWith('.csv') || file.type === 'text/csv' || file.type === 'application/vnd.ms-excel';
}

export function validateMedicationImportFile(file: File): string | null {
  if (!isMedicationCsvFile(file)) {
    return 'Please select a valid .csv file.';
  }

  if (file.size > MAX_MEDICATION_IMPORT_FILE_BYTES) {
    return `File is too large. Maximum size is ${MAX_MEDICATION_IMPORT_FILE_SIZE_LABEL}.`;
  }

  if (file.size === 0) {
    return 'The selected file is empty.';
  }

  return null;
}
