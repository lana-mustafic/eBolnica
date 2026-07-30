import { TestBed } from '@angular/core/testing';
import { AuthService } from '../auth.service';
import {
  buildMedicationWizardDraftOwnerKey,
  buildMedicationWizardDraftStorageKey,
  isMedicationWizardDraftExpired,
  MEDICATION_WIZARD_DRAFT_SESSION_KEY,
  MEDICATION_WIZARD_DRAFT_TTL_MS,
  MedicationWizardDraftService
} from './medication-wizard-draft.service';

describe('MedicationWizardDraftService', () => {
  let service: MedicationWizardDraftService;
  let authService: jasmine.SpyObj<AuthService>;
  let storage: Record<string, string>;

  const pharmacistUserId = 'pharmacist-42';
  const otherUserId = 'pharmacist-99';

  const formValue = {
    name: 'Draft Med',
    category: 'Antibiotics',
    description: 'Draft description',
    price: 12.5,
    stockQuantity: 20,
    minimumStockLevel: 5,
    dosageForm: 'Tablet',
    strength: '500mg',
    expiryDate: '2027-01-15',
    batchNumber: 'B-001',
    requiresPrescription: true,
    isActive: true,
    genericName: 'Generic',
    manufacturer: 'ACME'
  };

  beforeEach(() => {
    storage = {};
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getUserId']);

    spyOn(localStorage, 'getItem').and.callFake((key: string) => storage[key] ?? null);
    spyOn(localStorage, 'setItem').and.callFake((key: string, value: string) => {
      storage[key] = value;
    });
    spyOn(localStorage, 'removeItem').and.callFake((key: string) => {
      delete storage[key];
    });

    spyOn(sessionStorage, 'getItem').and.callFake((key: string) => storage[`session:${key}`] ?? null);
    spyOn(sessionStorage, 'setItem').and.callFake((key: string, value: string) => {
      storage[`session:${key}`] = value;
    });

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authService }]
    });

    service = TestBed.inject(MedicationWizardDraftService);
  });

  it('namespaces draft storage key by AuthService user id', () => {
    expect(service.getUserDraftStorageKey(pharmacistUserId))
      .toBe('medication-wizard-draft:user:pharmacist-42');
    expect(buildMedicationWizardDraftStorageKey(buildMedicationWizardDraftOwnerKey(pharmacistUserId, 'fallback')))
      .toBe('medication-wizard-draft:user:pharmacist-42');
  });

  it('saves draft to localStorage with timestamp for authenticated pharmacist', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);

    service.save({ currentStep: 2, formValue });

    const key = service.getUserDraftStorageKey(pharmacistUserId);
    expect(localStorage.setItem).toHaveBeenCalled();
    expect(storage[key]).toBeTruthy();

    const saved = JSON.parse(storage[key]);
    expect(saved.currentStep).toBe(2);
    expect(saved.formValue.name).toBe('Draft Med');
    expect(saved.ownerKey).toBe('user:pharmacist-42');
    expect(saved.savedAt).toMatch(/^\d{4}-\d{2}-\d{2}T/);
  });

  it('loads saved draft for current owner', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);
    service.save({ currentStep: 3, formValue });

    const loaded = service.load();

    expect(loaded).not.toBeNull();
    expect(loaded?.currentStep).toBe(3);
    expect(loaded?.formValue.name).toBe('Draft Med');
    expect(loaded?.ownerKey).toBe('user:pharmacist-42');
  });

  it('persists all wizard form fields and step index in localStorage', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);

    service.save({ currentStep: 2, formValue });

    const loaded = service.load();
    expect(loaded).not.toBeNull();
    expect(loaded?.currentStep).toBe(2);
    expect(loaded?.formValue).toEqual(formValue);

    const key = service.getUserDraftStorageKey(pharmacistUserId);
    const stored = JSON.parse(storage[key]);
    expect(stored.currentStep).toBe(2);
    expect(stored.formValue).toEqual(formValue);
  });

  it('uses session namespace when AuthService user id is unavailable', () => {
    authService.getUserId.and.returnValue(null);

    service.save({ currentStep: 1, formValue });

    const sessionKey = storage[`session:${MEDICATION_WIZARD_DRAFT_SESSION_KEY}`];
    expect(sessionKey).toBeTruthy();
    expect(storage[service.getDraftStorageKey(`session:${sessionKey}`)]).toBeTruthy();
  });

  it('hasDraft returns true only when a valid draft exists', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);
    expect(service.hasDraft()).toBeFalse();

    service.save({ currentStep: 1, formValue });

    expect(service.hasDraft()).toBeTrue();
  });

  it('clear removes draft from localStorage', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);
    service.save({ currentStep: 2, formValue });
    expect(service.hasDraft()).toBeTrue();

    service.clear();

    expect(service.hasDraft()).toBeFalse();
    expect(storage[service.getUserDraftStorageKey(pharmacistUserId)]).toBeUndefined();
  });

  it('does not load draft for a different pharmacist user', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);
    service.save({ currentStep: 2, formValue });

    authService.getUserId.and.returnValue(otherUserId);
    expect(service.load()).toBeNull();
    expect(service.hasDraft()).toBeFalse();
  });

  it('ignores and clears expired drafts', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);
    const staleSavedAt = new Date(Date.now() - MEDICATION_WIZARD_DRAFT_TTL_MS - 1000).toISOString();
    const key = service.getUserDraftStorageKey(pharmacistUserId);

    storage[key] = JSON.stringify({
      version: 1,
      savedAt: staleSavedAt,
      currentStep: 1,
      formValue,
      ownerKey: 'user:pharmacist-42'
    });

    expect(service.load()).toBeNull();
    expect(service.hasDraft()).toBeFalse();
    expect(storage[key]).toBeUndefined();
  });

  it('evaluateDraft marks drafts older than 7 days as expired', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);
    const staleSavedAt = new Date(Date.now() - MEDICATION_WIZARD_DRAFT_TTL_MS - 1000).toISOString();
    const key = service.getUserDraftStorageKey(pharmacistUserId);

    storage[key] = JSON.stringify({
      version: 1,
      savedAt: staleSavedAt,
      currentStep: 1,
      formValue,
      ownerKey: 'user:pharmacist-42'
    });

    const evaluation = service.evaluateDraft();

    expect(evaluation.status).toBe('expired');
    expect(evaluation.draft?.savedAt).toBe(staleSavedAt);
  });

  it('evaluateDraft keeps drafts within the 7 day TTL as valid', () => {
    authService.getUserId.and.returnValue(pharmacistUserId);
    service.save({ currentStep: 2, formValue });

    const evaluation = service.evaluateDraft();

    expect(evaluation.status).toBe('valid');
    expect(evaluation.draft?.currentStep).toBe(2);
  });

  it('treats invalid savedAt timestamps as expired', () => {
    const nowMs = Date.parse('2026-07-30T12:00:00.000Z');

    expect(isMedicationWizardDraftExpired('not-a-date', nowMs)).toBeTrue();
    expect(isMedicationWizardDraftExpired('2026-07-23T12:00:00.000Z', nowMs)).toBeFalse();
    expect(isMedicationWizardDraftExpired('2026-07-23T11:59:59.999Z', nowMs)).toBeTrue();
  });
});
