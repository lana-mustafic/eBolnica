import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { MedicationWizardComponent } from './medication-wizard.component';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { ConfirmDialogService } from '../../../shared/services/confirm-dialog.service';
import {
  MedicationWizardDraft,
  MedicationWizardDraftService
} from '../../../shared/services/pharmacy/medication-wizard-draft.service';

describe('MedicationWizardComponent draft banner', () => {
  let fixture: ComponentFixture<MedicationWizardComponent>;
  let component: MedicationWizardComponent;
  let draftService: jasmine.SpyObj<MedicationWizardDraftService>;
  let confirmDialog: jasmine.SpyObj<ConfirmDialogService>;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  const draft: MedicationWizardDraft = {
    version: 1,
    savedAt: '2026-07-30T10:15:00.000Z',
    currentStep: 2,
    ownerKey: 'user:pharmacist-42',
    formValue: {
      name: 'Draft Med',
      category: 'Antibiotics',
      description: 'Saved draft',
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
    }
  };

  beforeEach(async () => {
    draftService = jasmine.createSpyObj<MedicationWizardDraftService>('MedicationWizardDraftService', [
      'load',
      'save',
      'clear',
      'hasDraft',
      'evaluateDraft'
    ]);
    confirmDialog = jasmine.createSpyObj<ConfirmDialogService>('ConfirmDialogService', ['confirm']);
    pharmacyService = jasmine.createSpyObj<PharmacyService>('PharmacyService', [
      'checkMedicationNameAvailability',
      'createMedication'
    ]);

    pharmacyService.checkMedicationNameAvailability.and.returnValue(of({ isAvailable: true }));
    confirmDialog.confirm.and.returnValue(of(false));

    await TestBed.configureTestingModule({
      imports: [MedicationWizardComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: MedicationWizardDraftService, useValue: draftService },
        { provide: ConfirmDialogService, useValue: confirmDialog },
        { provide: PharmacyService, useValue: pharmacyService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationWizardComponent);
    component = fixture.componentInstance;
  });

  it('shows Continue draft and Discard draft banner when a draft exists on init', () => {
    draftService.evaluateDraft.and.returnValue({ status: 'valid', draft });

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.draft-banner');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('Continue draft');
    expect(banner.textContent).toContain('Discard draft');
    expect(component.showDraftBanner).toBeTrue();
    expect(component.pendingDraft).toEqual(draft);
  });

  it('does not show draft banner when no draft exists on init', () => {
    draftService.evaluateDraft.and.returnValue({ status: 'none', draft: null });

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.draft-banner')).toBeFalsy();
    expect(component.showDraftBanner).toBeFalse();
  });

  it('restores all saved fields and step when Continue draft is chosen', () => {
    draftService.evaluateDraft.and.returnValue({ status: 'valid', draft });

    fixture.detectChanges();
    component.continueDraft();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.draft-banner')).toBeFalsy();
    expect(component.currentStep).toBe(2);
    expect(component.wizardForm.getRawValue()).toEqual(draft.formValue);
  });

  it('does not trigger autosave while restoring draft', () => {
    draftService.evaluateDraft.and.returnValue({ status: 'valid', draft });

    fixture.detectChanges();
    component.continueDraft();

    expect(draftService.save).not.toHaveBeenCalled();
  });

  it('clears draft and hides banner when Discard draft is confirmed', () => {
    draftService.evaluateDraft.and.returnValue({ status: 'valid', draft });
    confirmDialog.confirm.and.returnValue(of(true));

    fixture.detectChanges();
    component.discardDraft();
    fixture.detectChanges();

    expect(draftService.clear).toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.draft-banner')).toBeFalsy();
    expect(component.pendingDraft).toBeNull();
  });

  it('keeps draft banner visible when Discard draft is cancelled', () => {
    draftService.evaluateDraft.and.returnValue({ status: 'valid', draft });
    confirmDialog.confirm.and.returnValue(of(false));

    fixture.detectChanges();
    component.discardDraft();
    fixture.detectChanges();

    expect(draftService.clear).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.draft-banner')).toBeTruthy();
  });

  it('prompts to discard stale drafts older than 7 days on init', () => {
    draftService.evaluateDraft.and.returnValue({ status: 'expired', draft });

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.draft-banner')).toBeFalsy();
    expect(confirmDialog.confirm).toHaveBeenCalledWith(jasmine.objectContaining({
      title: 'Draft expired'
    }));
  });

  it('clears stale draft after expired prompt is dismissed', () => {
    draftService.evaluateDraft.and.returnValue({ status: 'expired', draft });
    confirmDialog.confirm.and.returnValue(of(false));

    fixture.detectChanges();

    expect(draftService.clear).toHaveBeenCalled();
    expect(component.showDraftBanner).toBeFalse();
  });
});

describe('MedicationWizardComponent draft cleanup on submit', () => {
  let fixture: ComponentFixture<MedicationWizardComponent>;
  let component: MedicationWizardComponent;
  let draftService: jasmine.SpyObj<MedicationWizardDraftService>;
  let confirmDialog: jasmine.SpyObj<ConfirmDialogService>;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;
  let router: Router;

  beforeEach(async () => {
    draftService = jasmine.createSpyObj<MedicationWizardDraftService>('MedicationWizardDraftService', [
      'load',
      'save',
      'clear',
      'hasDraft',
      'evaluateDraft'
    ]);
    confirmDialog = jasmine.createSpyObj<ConfirmDialogService>('ConfirmDialogService', ['confirm']);
    pharmacyService = jasmine.createSpyObj<PharmacyService>('PharmacyService', [
      'checkMedicationNameAvailability',
      'createMedication'
    ]);

    draftService.evaluateDraft.and.returnValue({ status: 'none', draft: null });
    pharmacyService.checkMedicationNameAvailability.and.returnValue(of({ isAvailable: true }));
    confirmDialog.confirm.and.returnValue(of(false));

    await TestBed.configureTestingModule({
      imports: [MedicationWizardComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: MedicationWizardDraftService, useValue: draftService },
        { provide: ConfirmDialogService, useValue: confirmDialog },
        { provide: PharmacyService, useValue: pharmacyService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationWizardComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  });

  function fillValidForm(): void {
    component.wizardForm.patchValue({
      name: 'New Medication',
      category: 'Antibiotics',
      description: 'Description',
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
    });
  }

  it('clears draft after successful createMedication', fakeAsync(() => {
    pharmacyService.createMedication.and.returnValue(of({ id: 1 } as never));

    fixture.detectChanges();
    fillValidForm();
    tick(400);
    fixture.detectChanges();

    expect(component.wizardForm.valid).toBeTrue();

    component.onSubmit();
    tick();

    expect(draftService.clear).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/pharmacy/medications']);
  }));

  it('does not clear draft when createMedication fails', fakeAsync(() => {
    pharmacyService.createMedication.and.returnValue(
      throwError(() => ({ error: { message: 'Create failed' } }))
    );

    fixture.detectChanges();
    fillValidForm();
    tick(400);
    fixture.detectChanges();

    component.onSubmit();
    tick();

    expect(draftService.clear).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  }));

  it('does not persist draft on destroy after successful create', fakeAsync(() => {
    pharmacyService.createMedication.and.returnValue(of({ id: 1 } as never));

    fixture.detectChanges();
    fillValidForm();
    tick(400);
    fixture.detectChanges();

    component.onSubmit();
    tick();
    fixture.destroy();

    expect(draftService.save).not.toHaveBeenCalled();
  }));
});
