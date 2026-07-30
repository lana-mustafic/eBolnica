import {
  buildMedicationWizardDraftRestoreState,
  buildMedicationWizardDraftSavePayload,
  MEDICATION_WIZARD_FORM_FIELD_KEYS,
  normalizeMedicationWizardStepIndex,
  pickMedicationWizardDraftFormPatch,
  toMedicationWizardDraftFormValue
} from './medication-wizard-autosave.util';

describe('medication-wizard-autosave.util', () => {
  const rawFormValue = {
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
  };

  it('maps wizard form raw values into draft form value shape', () => {
    expect(toMedicationWizardDraftFormValue(rawFormValue)).toEqual(rawFormValue);
  });

  it('builds save payload with normalized step index and all wizard fields', () => {
    const payload = buildMedicationWizardDraftSavePayload(2, rawFormValue, 3);

    expect(payload.currentStep).toBe(2);
    expect(payload.formValue).toEqual(rawFormValue);
    expect(Object.keys(payload.formValue).sort()).toEqual([...MEDICATION_WIZARD_FORM_FIELD_KEYS].sort());
  });

  it('clamps step index into valid wizard range before persistence', () => {
    expect(normalizeMedicationWizardStepIndex(0, 3)).toBe(1);
    expect(normalizeMedicationWizardStepIndex(99, 3)).toBe(3);
    expect(buildMedicationWizardDraftSavePayload(5, rawFormValue, 3).currentStep).toBe(3);
  });

  it('builds restore state with normalized step and all wizard fields', () => {
    const restoreState = buildMedicationWizardDraftRestoreState(
      { currentStep: 5, formValue: rawFormValue },
      3
    );

    expect(restoreState.currentStep).toBe(3);
    expect(restoreState.formValue).toEqual(rawFormValue);
    expect(Object.keys(restoreState.formValue).sort()).toEqual([...MEDICATION_WIZARD_FORM_FIELD_KEYS].sort());
  });

  it('picks explicit form patch keys for draft restore', () => {
    expect(pickMedicationWizardDraftFormPatch(rawFormValue)).toEqual(rawFormValue);
    expect(Object.keys(pickMedicationWizardDraftFormPatch(rawFormValue)).sort())
      .toEqual([...MEDICATION_WIZARD_FORM_FIELD_KEYS].sort());
  });
});
