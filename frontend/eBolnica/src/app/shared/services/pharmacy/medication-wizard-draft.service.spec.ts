import { TestBed } from '@angular/core/testing';
import { AuthService } from '../auth.service';
import {
  MEDICATION_WIZARD_DRAFT_SESSION_KEY,
  MEDICATION_WIZARD_DRAFT_TTL_MS,
  MedicationWizardDraftService
} from './medication-wizard-draft.service';

describe('MedicationWizardDraftService', () => {
  let service: MedicationWizardDraftService;
  let authService: jasmine.SpyObj<AuthService>;
  let storage: Record<string, string>;

  const userToken = createToken({ sub: 'pharmacist-42' });
  const otherUserToken = createToken({ sub: 'pharmacist-99' });

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
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getToken']);

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

  it('saves draft to localStorage with timestamp for authenticated pharmacist', () => {
    authService.getToken.and.returnValue(userToken);

    service.save({ currentStep: 2, formValue });

    const key = service.getDraftStorageKey('pharmacist-42');
    expect(localStorage.setItem).toHaveBeenCalled();
    expect(storage[key]).toBeTruthy();

    const saved = JSON.parse(storage[key]);
    expect(saved.currentStep).toBe(2);
    expect(saved.formValue.name).toBe('Draft Med');
    expect(saved.ownerKey).toBe('pharmacist-42');
    expect(saved.savedAt).toMatch(/^\d{4}-\d{2}-\d{2}T/);
  });

  it('loads saved draft for current owner', () => {
    authService.getToken.and.returnValue(userToken);
    service.save({ currentStep: 3, formValue });

    const loaded = service.load();

    expect(loaded).not.toBeNull();
    expect(loaded?.currentStep).toBe(3);
    expect(loaded?.formValue.name).toBe('Draft Med');
    expect(loaded?.ownerKey).toBe('pharmacist-42');
  });

  it('persists all wizard form fields and step index in localStorage', () => {
    authService.getToken.and.returnValue(userToken);

    service.save({ currentStep: 2, formValue });

    const loaded = service.load();
    expect(loaded).not.toBeNull();
    expect(loaded?.currentStep).toBe(2);
    expect(loaded?.formValue).toEqual(formValue);

    const key = service.getDraftStorageKey('pharmacist-42');
    const stored = JSON.parse(storage[key]);
    expect(stored.currentStep).toBe(2);
    expect(stored.formValue).toEqual(formValue);
  });

  it('uses session key when no auth token is available', () => {
    authService.getToken.and.returnValue(null);

    service.save({ currentStep: 1, formValue });

    const sessionKey = storage[`session:${MEDICATION_WIZARD_DRAFT_SESSION_KEY}`];
    expect(sessionKey).toMatch(/^session-/);
    expect(storage[service.getDraftStorageKey(sessionKey)]).toBeTruthy();
  });

  it('hasDraft returns true only when a valid draft exists', () => {
    authService.getToken.and.returnValue(userToken);
    expect(service.hasDraft()).toBeFalse();

    service.save({ currentStep: 1, formValue });

    expect(service.hasDraft()).toBeTrue();
  });

  it('clear removes draft from localStorage', () => {
    authService.getToken.and.returnValue(userToken);
    service.save({ currentStep: 2, formValue });
    expect(service.hasDraft()).toBeTrue();

    service.clear();

    expect(service.hasDraft()).toBeFalse();
    expect(storage[service.getDraftStorageKey('pharmacist-42')]).toBeUndefined();
  });

  it('does not load draft for a different pharmacist user', () => {
    authService.getToken.and.returnValue(userToken);
    service.save({ currentStep: 2, formValue });

    authService.getToken.and.returnValue(otherUserToken);
    expect(service.load()).toBeNull();
    expect(service.hasDraft()).toBeFalse();
  });

  it('ignores and clears expired drafts', () => {
    authService.getToken.and.returnValue(userToken);
    const staleSavedAt = new Date(Date.now() - MEDICATION_WIZARD_DRAFT_TTL_MS - 1000).toISOString();
    const key = service.getDraftStorageKey('pharmacist-42');

    storage[key] = JSON.stringify({
      version: 1,
      savedAt: staleSavedAt,
      currentStep: 1,
      formValue,
      ownerKey: 'pharmacist-42'
    });

    expect(service.load()).toBeNull();
    expect(service.hasDraft()).toBeFalse();
    expect(storage[key]).toBeUndefined();
  });
});

function createToken(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.signature`;
}
