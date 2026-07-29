import { MedicationDto } from '../../models/medication.dto';
import {
  buildCsvContent,
  escapeCsvValue,
  formatLocaleDateForCsv,
  getDatedExportFilename
} from './csv.util';

export const INVENTORY_EXPORT_CSV_HEADERS = [
  'Medication Name',
  'Generic Name',
  'Category',
  'Manufacturer',
  'Current Stock',
  'Minimum Stock Level',
  'Stock Status',
  'Price',
  'Expiry Date',
  'Days Until Expiry',
  'Expiry Status',
  'Batch Number',
  'Last Updated'
] as const;

type InventoryStockStatus = 'adequate' | 'low' | 'critical' | 'out-of-stock';
type InventoryExpiryStatus = 'good' | 'warning' | 'critical' | 'expired';

function calculateInventoryStockStatus(stock: number, minimum: number): InventoryStockStatus {
  if (stock === 0) {
    return 'out-of-stock';
  }

  if (stock < 5) {
    return 'critical';
  }

  if (stock < minimum) {
    return 'low';
  }

  return 'adequate';
}

function getInventoryStockStatusText(stock: number, minimum: number): string {
  switch (calculateInventoryStockStatus(stock, minimum)) {
    case 'adequate':
      return 'Adequate';
    case 'low':
      return 'Low Stock';
    case 'critical':
      return 'Critical';
    case 'out-of-stock':
      return 'Out of Stock';
    default:
      return 'Unknown';
  }
}

function calculateInventoryExpiryStatus(expiryDate: string | undefined): InventoryExpiryStatus {
  if (!expiryDate) {
    return 'good';
  }

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const expiry = new Date(expiryDate);
  expiry.setHours(0, 0, 0, 0);

  const daysUntilExpiry = Math.floor((expiry.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

  if (daysUntilExpiry < 0) {
    return 'expired';
  }

  if (daysUntilExpiry < 30) {
    return 'critical';
  }

  if (daysUntilExpiry < 90) {
    return 'warning';
  }

  return 'good';
}

function getInventoryExpiryStatusText(expiryDate: string | undefined): string {
  switch (calculateInventoryExpiryStatus(expiryDate)) {
    case 'good':
      return 'Good';
    case 'warning':
      return 'Warning';
    case 'critical':
      return 'Expiring Soon';
    case 'expired':
      return 'Expired';
    default:
      return 'Unknown';
  }
}

function getDaysUntilExpiry(expiryDate: string | undefined): number {
  if (!expiryDate) {
    return -1;
  }

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const expiry = new Date(expiryDate);
  expiry.setHours(0, 0, 0, 0);

  return Math.floor((expiry.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
}

export function buildInventoryExportCsv(items: MedicationDto[]): string {
  const rows = items.map(item => [
    escapeCsvValue(item.name),
    escapeCsvValue(item.genericName || ''),
    escapeCsvValue(item.category || ''),
    escapeCsvValue(item.manufacturer || ''),
    item.stockQuantity.toString(),
    item.minimumStockLevel.toString(),
    getInventoryStockStatusText(item.stockQuantity, item.minimumStockLevel),
    item.price.toString(),
    item.expiryDate ? formatLocaleDateForCsv(item.expiryDate) : '',
    item.expiryDate ? getDaysUntilExpiry(item.expiryDate).toString() : '',
    getInventoryExpiryStatusText(item.expiryDate),
    escapeCsvValue(item.batchNumber || ''),
    item.updatedAt
      ? formatLocaleDateForCsv(item.updatedAt)
      : formatLocaleDateForCsv(item.createdAt)
  ]);

  return buildCsvContent(INVENTORY_EXPORT_CSV_HEADERS, rows);
}

export function getInventoryExportFilename(date: Date = new Date()): string {
  return getDatedExportFilename('pharmacy-inventory', date);
}
