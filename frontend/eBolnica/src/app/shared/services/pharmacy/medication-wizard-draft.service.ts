import { inject, Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { AuthService } from '../auth.service';

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

interface JwtUserPayload {
  sub?: string;
}

@Injectable({
  providedIn: 'root'
})
export class MedicationWizardDraftService {
  private authService = inject(AuthService);

  save(payload: MedicationWizardDraftSavePayload): void {
    const ownerKey = this.getOwnerKey();
    const draft: MedicationWizardDraft = {
      version: MEDICATION_WIZARD_DRAFT_VERSION,
      savedAt: new Date().toISOString(),
      currentStep: payload.currentStep,
      formValue: payload.formValue,
      ownerKey
    };

    this.writeDraft(ownerKey, draft);
  }

  load(): MedicationWizardDraft | null {
    const draft = this.readDraftForCurrentOwner();
    if (!draft) {
      return null;
    }

    if (this.isDraftExpired(draft)) {
      this.clear();
      return null;
    }

    return draft;
  }

  clear(): void {
    const ownerKey = this.getOwnerKey();
    this.removeDraft(ownerKey);
  }

  hasDraft(): boolean {
    return this.load() !== null;
  }

  getDraftStorageKey(ownerKey: string): string {
    return `${MEDICATION_WIZARD_DRAFT_STORAGE_PREFIX}:${ownerKey}`;
  }

  isDraftExpired(draft: MedicationWizardDraft, nowMs: number = Date.now()): boolean {
    const savedAtMs = Date.parse(draft.savedAt);
    if (Number.isNaN(savedAtMs)) {
      return true;
    }

    return nowMs - savedAtMs > MEDICATION_WIZARD_DRAFT_TTL_MS;
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
      const raw = localStorage.getItem(this.getDraftStorageKey(ownerKey));
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
      localStorage.setItem(this.getDraftStorageKey(ownerKey), JSON.stringify(draft));
    } catch (error) {
      console.warn('[MedicationWizardDraftService] Failed to save draft:', error);
    }
  }

  private removeDraft(ownerKey: string): void {
    try {
      localStorage.removeItem(this.getDraftStorageKey(ownerKey));
    } catch (error) {
      console.warn('[MedicationWizardDraftService] Failed to clear draft:', error);
    }
  }

  private getOwnerKey(): string {
    return this.getUserIdFromToken() ?? this.getOrCreateSessionKey();
  }

  private getUserIdFromToken(): string | null {
    const token = this.authService.getToken();
    if (!token) {
      return null;
    }

    try {
      const payload = jwtDecode<JwtUserPayload>(token);
      return payload.sub?.trim() || null;
    } catch {
      return null;
    }
  }

  private getOrCreateSessionKey(): string {
    try {
      const existing = sessionStorage.getItem(MEDICATION_WIZARD_DRAFT_SESSION_KEY);
      if (existing) {
        return existing;
      }

      const sessionKey = `session-${this.createRandomId()}`;
      sessionStorage.setItem(MEDICATION_WIZARD_DRAFT_SESSION_KEY, sessionKey);
      return sessionKey;
    } catch {
      return `session-${this.createRandomId()}`;
    }
  }

  private createRandomId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }

    return `${Date.now()}-${Math.random().toString(36).slice(2, 11)}`;
  }
}
