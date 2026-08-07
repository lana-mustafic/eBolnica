import { inject, Injectable } from '@angular/core';
import { CurrentUserService } from '../../../core/services/auth/current-user.service';

export const MEDICATION_WIZARD_DRAFT_STORAGE_PREFIX = 'medication-wizard-draft';
export const MEDICATION_WIZARD_DRAFT_SESSION_KEY = 'medication-wizard-session-key';
export const MEDICATION_WIZARD_DRAFT_VERSION = 1;
export const MEDICATION_WIZARD_DRAFT_TTL_MS = 7 * 24 * 60 * 60 * 1000;

export interface MedicationWizardDraftFormValue {
  name: string;
  category: string;
  description: string;
  price: number;
  stockQuantity: number;
  minimumStockLevel: number;
  dosageForm: string;
  strength: string;
  expiryDate: string;
  batchNumber: string;
  requiresPrescription: boolean;
  isActive: boolean;
  genericName: string;
  manufacturer: string;
}

export interface MedicationWizardDraft {
  version: number;
  savedAt: string;
  currentStep: number;
  formValue: MedicationWizardDraftFormValue;
  ownerKey: string;
}

export interface MedicationWizardDraftSavePayload {
  currentStep: number;
  formValue: MedicationWizardDraftFormValue;
}

export function buildMedicationWizardDraftOwnerKey(userId: string | null, sessionKey: string): string {
  if (userId) {
    return `user:${userId}`;
  }

  return `session:${sessionKey}`;
}

export function buildMedicationWizardDraftStorageKey(ownerKey: string): string {
  return `${MEDICATION_WIZARD_DRAFT_STORAGE_PREFIX}:${ownerKey}`;
}

export type MedicationWizardDraftStatus = 'none' | 'valid' | 'expired';

export interface MedicationWizardDraftEvaluation {
  status: MedicationWizardDraftStatus;
  draft: MedicationWizardDraft | null;
}

export function isMedicationWizardDraftExpired(
  savedAt: string,
  nowMs: number = Date.now(),
  ttlMs: number = MEDICATION_WIZARD_DRAFT_TTL_MS
): boolean {
  const savedAtMs = Date.parse(savedAt);
  if (Number.isNaN(savedAtMs)) {
    return true;
  }

  return nowMs - savedAtMs > ttlMs;
}

@Injectable({
  providedIn: 'root',
})
export class MedicationWizardDraftService {
  private currentUser = inject(CurrentUserService);

  save(payload: MedicationWizardDraftSavePayload): void {
    const ownerKey = this.getOwnerKey();
    const draft: MedicationWizardDraft = {
      version: MEDICATION_WIZARD_DRAFT_VERSION,
      savedAt: new Date().toISOString(),
      currentStep: payload.currentStep,
      formValue: payload.formValue,
      ownerKey,
    };

    this.writeDraft(ownerKey, draft);
  }

  evaluateDraft(): MedicationWizardDraftEvaluation {
    const draft = this.readDraftForCurrentOwner();
    if (!draft) {
      return { status: 'none', draft: null };
    }

    if (this.isDraftExpired(draft)) {
      return { status: 'expired', draft };
    }

    return { status: 'valid', draft };
  }

  clear(): void {
    const ownerKey = this.getOwnerKey();
    this.removeDraft(ownerKey);
  }

  isDraftExpired(draft: MedicationWizardDraft, nowMs: number = Date.now()): boolean {
    return isMedicationWizardDraftExpired(draft.savedAt, nowMs);
  }

  private readDraftForCurrentOwner(): MedicationWizardDraft | null {
    const ownerKey = this.getOwnerKey();
    const draft = this.readDraft(ownerKey);

    if (!draft || draft.ownerKey !== ownerKey) {
      return null;
    }

    return draft;
  }

  private readDraft(ownerKey: string): MedicationWizardDraft | null {
    try {
      const raw = localStorage.getItem(buildMedicationWizardDraftStorageKey(ownerKey));
      if (!raw) {
        return null;
      }

      const parsed = JSON.parse(raw) as MedicationWizardDraft;
      if (!parsed || typeof parsed !== 'object') {
        return null;
      }

      if (parsed.version !== MEDICATION_WIZARD_DRAFT_VERSION) {
        return null;
      }

      return parsed;
    } catch {
      return null;
    }
  }

  private writeDraft(ownerKey: string, draft: MedicationWizardDraft): void {
    try {
      localStorage.setItem(buildMedicationWizardDraftStorageKey(ownerKey), JSON.stringify(draft));
    } catch (error) {
      console.warn('[MedicationWizardDraftService] Failed to save draft:', error);
    }
  }

  private removeDraft(ownerKey: string): void {
    try {
      localStorage.removeItem(buildMedicationWizardDraftStorageKey(ownerKey));
    } catch (error) {
      console.warn('[MedicationWizardDraftService] Failed to clear draft:', error);
    }
  }

  private getOwnerKey(): string {
    const userId = this.currentUser.snapshot?.userId;
    return buildMedicationWizardDraftOwnerKey(
      userId != null ? String(userId) : null,
      this.getOrCreateSessionKey()
    );
  }

  private getOrCreateSessionKey(): string {
    try {
      const existing = sessionStorage.getItem(MEDICATION_WIZARD_DRAFT_SESSION_KEY);
      if (existing) {
        return existing;
      }

      const sessionKey = this.createRandomId();
      sessionStorage.setItem(MEDICATION_WIZARD_DRAFT_SESSION_KEY, sessionKey);
      return sessionKey;
    } catch {
      return this.createRandomId();
    }
  }

  private createRandomId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }

    return `${Date.now()}-${Math.random().toString(36).slice(2, 11)}`;
  }
}
