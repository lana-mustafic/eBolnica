import { toMedicationWizardDraftFormValue } from './medication-wizard-autosave.util';

describe('medication-wizard-autosave.util', () => {
  it('maps wizard form raw values into draft form value shape', () => {
    const draftValue = toMedicationWizardDraftFormValue({
      name: 'Draft Med',
      category: 'Antibiotics',
      description: 'Notes',
      price: 9.99,
      stockQuantity: 12,
      minimumStockLevel: 4,
      dosageForm: 'Tablet',
      strength: '250mg',
      expiryDate: '2027-05-01',
      batchNumber: 'B-12',
      requiresPrescription: false,
      isActive: true,
      genericName: 'Generic',
      manufacturer: 'ACME'
    });

    expect(draftValue).toEqual({
      name: 'Draft Med',
      category: 'Antibiotics',
      description: 'Notes',
      price: 9.99,
      stockQuantity: 12,
      minimumStockLevel: 4,
      dosageForm: 'Tablet',
      strength: '250mg',
      expiryDate: '2027-05-01',
      batchNumber: 'B-12',
      requiresPrescription: false,
      isActive: true,
      genericName: 'Generic',
      manufacturer: 'ACME'
    });
  });
});
