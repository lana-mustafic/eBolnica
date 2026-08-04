import { MedicationDto } from '../../models/medication.dto';
import {
  buildCsvContent,
  downloadCsv,
  escapeCsvValue,
  formatIsoDateForCsv,
  getDatedExportFilename
} from './csv.util';

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

/** @deprecated Use escapeCsvValue from csv.util */
export const escapeMedicationCsvValue = escapeCsvValue;

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
    escapeCsvValue(medication.name),
    escapeCsvValue(medication.genericName || ''),
    escapeCsvValue(medication.category || ''),
    escapeCsvValue(medication.manufacturer || ''),
    escapeCsvValue(medication.description || ''),
    medication.price.toString(),
    medication.stockQuantity.toString(),
    medication.minimumStockLevel.toString(),
    medication.expiryDate ? formatIsoDateForCsv(medication.expiryDate) : '',
    escapeCsvValue(medication.batchNumber || ''),
    escapeCsvValue(medication.dosageForm || ''),
    escapeCsvValue(medication.strength || ''),
    medication.requiresPrescription ? 'Yes' : 'No',
    medication.isActive ? 'Yes' : 'No',
    escapeCsvValue(getMedicationStockStatusLabel(medication))
  ]);

  return buildCsvContent(MEDICATION_EXPORT_CSV_HEADERS, rows);
}

export function buildMedicationImportTemplateCsv(): string {
  const exampleRow = MEDICATION_IMPORT_TEMPLATE_EXAMPLE_ROW.map(value => escapeCsvValue(value));
  return buildCsvContent(MEDICATION_IMPORT_CSV_HEADERS, [exampleRow]);
}

export function getMedicationExportFilename(date: Date = new Date()): string {
  return getDatedExportFilename('pharmacy-medications', date);
}

/** @deprecated Use downloadCsv from csv.util */
export const downloadMedicationCsv = downloadCsv;

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
