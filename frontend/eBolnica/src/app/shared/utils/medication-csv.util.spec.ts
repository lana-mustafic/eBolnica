import {
  buildMedicationExportCsv,
  buildMedicationImportTemplateCsv,
  escapeMedicationCsvValue,
  validateMedicationImportFile
} from './medication-csv.util';
import { MedicationDto } from '../../models/medication.dto';

describe('medication-csv.util', () => {
  it('should escape CSV values containing commas', () => {
    expect(escapeMedicationCsvValue('Pain, relief')).toBe('"Pain, relief"');
  });

  it('should include import headers in template CSV', () => {
    const csv = buildMedicationImportTemplateCsv();
    expect(csv.split('\n')[0]).toContain('Name');
    expect(csv.split('\n')[0]).toContain('Expiry Date');
    expect(csv.split('\n').length).toBe(2);
  });

  it('should build export CSV with status column', () => {
    const medication: MedicationDto = {
      id: 1,
      name: 'Paracetamol',
      price: 9.99,
      stockQuantity: 10,
      minimumStockLevel: 5,
      isActive: true,
      requiresPrescription: false,
      category: 'Painkillers',
      createdAt: '2026-01-01'
    };

    const csv = buildMedicationExportCsv([medication]);
    const lines = csv.split('\n');
    expect(lines[0]).toContain('Status');
    expect(lines[1]).toContain('Paracetamol');
    expect(lines[1]).toContain('Active');
  });

  it('should reject non-csv import files', () => {
    const file = new File(['data'], 'medications.txt', { type: 'text/plain' });
    expect(validateMedicationImportFile(file)).toContain('valid .csv file');
  });
});
