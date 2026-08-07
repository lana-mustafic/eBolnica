import { normalizeDosageForm } from '../constants/medication-dosage-forms.constant';
import { normalizeMedicationCategory } from '../constants/medication-categories.constant';
import {
  MedicationWizardDraft,
  MedicationWizardDraftFormValue,
  MedicationWizardDraftSavePayload,
} from '../services/medication-wizard-draft.service';

export const MEDICATION_WIZARD_AUTOSAVE_DEBOUNCE_MS = 2000;

export function normalizeMedicationWizardStepIndex(step: number, totalSteps: number): number {
  if (!Number.isFinite(step)) {
    return 1;
  }

  return Math.min(Math.max(Math.trunc(step), 1), totalSteps);
}

export function toMedicationWizardDraftFormValue(
  raw: Record<string, unknown>
): MedicationWizardDraftFormValue {
  return {
    name: String(raw['name'] ?? ''),
    category: normalizeMedicationCategory(String(raw['category'] ?? '')),
    description: String(raw['description'] ?? ''),
    price: Number(raw['price'] ?? 0),
    stockQuantity: Number(raw['stockQuantity'] ?? 0),
    minimumStockLevel: Number(raw['minimumStockLevel'] ?? 0),
    dosageForm: normalizeDosageForm(String(raw['dosageForm'] ?? '')),
    strength: String(raw['strength'] ?? ''),
    expiryDate: String(raw['expiryDate'] ?? ''),
    batchNumber: String(raw['batchNumber'] ?? ''),
    requiresPrescription: Boolean(raw['requiresPrescription'] ?? true),
    isActive: Boolean(raw['isActive'] ?? true),
    genericName: String(raw['genericName'] ?? ''),
    manufacturer: String(raw['manufacturer'] ?? ''),
  };
}

export function buildMedicationWizardDraftSavePayload(
  currentStep: number,
  rawFormValue: Record<string, unknown>,
  totalSteps: number
): MedicationWizardDraftSavePayload {
  return {
    currentStep: normalizeMedicationWizardStepIndex(currentStep, totalSteps),
    formValue: toMedicationWizardDraftFormValue(rawFormValue),
  };
}

export interface MedicationWizardDraftRestoreState {
  currentStep: number;
  formValue: MedicationWizardDraftFormValue;
}

export function buildMedicationWizardDraftRestoreState(
  draft: Pick<MedicationWizardDraft, 'currentStep' | 'formValue'>,
  totalSteps: number
): MedicationWizardDraftRestoreState {
  return {
    currentStep: normalizeMedicationWizardStepIndex(draft.currentStep, totalSteps),
    formValue: toMedicationWizardDraftFormValue({ ...draft.formValue }),
  };
}

export function pickMedicationWizardDraftFormPatch(
  formValue: MedicationWizardDraftFormValue
): MedicationWizardDraftFormValue {
  return { ...formValue };
}
