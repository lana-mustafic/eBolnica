import { buildInventoryExportCsv, getInventoryExportFilename } from './inventory-csv.util';
import { MedicationDto } from '../../models/medication.dto';

describe('inventory-csv.util', () => {
  it('should build inventory export CSV with expected headers', () => {
    const item: MedicationDto = {
      id: 1,
      name: 'Paracetamol',
      price: 9.99,
      stockQuantity: 0,
      minimumStockLevel: 20,
      isActive: true,
      requiresPrescription: false,
      category: 'Painkillers',
      createdAt: '2026-01-01T00:00:00Z'
    };

    const csv = buildInventoryExportCsv([item]);
    const lines = csv.split('\n');

    expect(lines[0]).toContain('Medication Name');
    expect(lines[0]).toContain('Expiry Status');
    expect(lines[1]).toContain('Paracetamol');
    expect(lines[1]).toContain('Out of Stock');
  });

  it('should build inventory export filename', () => {
    expect(getInventoryExportFilename(new Date('2026-03-15T12:00:00Z')))
      .toBe('pharmacy-inventory-2026-03-15.csv');
  });
});
