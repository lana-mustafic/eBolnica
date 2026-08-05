import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, map, Subscription } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationUpsertCommand } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { medicationNameAsyncValidator } from '../../../shared/validators/medication-name-async.validator';
import { DialogButton, DialogType } from '../../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import {
  MedicationWizardDraft,
  MedicationWizardDraftService,
} from '../../services/medication-wizard-draft.service';
import {
  buildMedicationWizardDraftRestoreState,
  buildMedicationWizardDraftSavePayload,
  MEDICATION_WIZARD_AUTOSAVE_DEBOUNCE_MS,
  pickMedicationWizardDraftFormPatch,
} from '../medication-wizard-autosave.util';

@Component({
  selector: 'app-medication-wizard',
  standalone: false,
  templateUrl: './medication-wizard.component.html',
  styleUrl: './medication-wizard.component.scss',
})
export class MedicationWizardComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private pharmacyApi = inject(PharmacyApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private draftService = inject(MedicationWizardDraftService);
  private dialog = inject(DialogHelperService);

  step = 1;
  readonly totalSteps = 3;
  isSaving = false;
  showDraftBanner = false;
  pendingDraft: MedicationWizardDraft | null = null;

  private autosaveSubscription?: Subscription;
  private draftPromptResolved = false;
  private suppressDraftPersist = false;

  categories = ['Analgesics', 'Antibiotics', 'Cardiovascular', 'Diabetes', 'Other'];
  dosageForms = ['Tablet', 'Capsule', 'Liquid', 'Injection'];

  form = this.fb.group({
    name: [
      '',
      [Validators.required, Validators.minLength(3)],
      [
        medicationNameAsyncValidator((name, excludeId) =>
          this.pharmacyApi.checkName(name, excludeId).pipe(map((res) => ({ isAvailable: res.isAvailable })))
        ),
      ],
    ],
    genericName: [''],
    category: ['', Validators.required],
    manufacturer: [''],
    description: [''],
    price: [0, [Validators.required, Validators.min(0.01)]],
    stockQuantity: [0, [Validators.required, Validators.min(0)]],
    minimumStockLevel: [10, [Validators.required, Validators.min(0)]],
    expiryDate: ['', Validators.required],
    batchNumber: [''],
    dosageForm: [''],
    strength: [''],
    requiresPrescription: [true],
    isActive: [true],
  });

  ngOnInit(): void {
    this.initializeDraftPrompt();

    this.autosaveSubscription = this.form.valueChanges
      .pipe(debounceTime(MEDICATION_WIZARD_AUTOSAVE_DEBOUNCE_MS))
      .subscribe(() => this.persistDraft());
  }

  ngOnDestroy(): void {
    if (this.draftPromptResolved && !this.suppressDraftPersist) {
      this.persistDraft();
    }
    this.autosaveSubscription?.unsubscribe();
  }

  get nameControl() {
    return this.form.get('name');
  }

  continueDraft(): void {
    if (!this.pendingDraft) {
      return;
    }

    this.restoreDraft(this.pendingDraft);
    this.showDraftBanner = false;
    this.draftPromptResolved = true;
    this.pendingDraft = null;
  }

  discardDraft(): void {
    this.dialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Odbaci draft',
        message: 'Jeste li sigurni da želite odbaciti sačuvani draft? Ova radnja se ne može poništiti.',
        buttons: [
          { type: DialogButton.CANCEL, label: 'Zadrži draft' },
          { type: DialogButton.DELETE, label: 'Odbaci', color: 'warn' },
        ],
      })
      .subscribe((result) => {
        if (result?.button !== DialogButton.DELETE) {
          return;
        }

        this.draftService.clear();
        this.pendingDraft = null;
        this.showDraftBanner = false;
        this.draftPromptResolved = true;
      });
  }

  getDraftSavedAtLabel(): string | null {
    if (!this.pendingDraft?.savedAt) {
      return null;
    }

    const savedAt = new Date(this.pendingDraft.savedAt);
    if (Number.isNaN(savedAt.getTime())) {
      return null;
    }

    return savedAt.toLocaleString('bs-BA', {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  next(): void {
    if (this.step === 1 && !this.isStepValid(['name', 'category', 'genericName', 'manufacturer', 'description'])) return;
    if (this.step === 2 && !this.isStepValid(['price', 'stockQuantity', 'minimumStockLevel', 'expiryDate', 'batchNumber', 'dosageForm', 'strength'])) return;
    this.step++;
    this.persistDraft();
  }

  back(): void {
    if (this.step > 1) {
      this.step--;
      this.persistDraft();
    }
  }

  submit(): void {
    if (this.form.invalid || this.form.pending) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const body: MedicationUpsertCommand = {
      name: raw.name!,
      genericName: raw.genericName || null,
      description: raw.description || null,
      manufacturer: raw.manufacturer || null,
      price: raw.price!,
      stockQuantity: raw.stockQuantity!,
      minimumStockLevel: raw.minimumStockLevel!,
      expiryDate: new Date(raw.expiryDate!).toISOString(),
      batchNumber: raw.batchNumber || null,
      isActive: raw.isActive!,
      requiresPrescription: raw.requiresPrescription!,
      category: raw.category!,
      dosageForm: raw.dosageForm || null,
      strength: raw.strength || null,
    };

    this.isSaving = true;
    this.pharmacyApi.createMedication(body).subscribe({
      next: (created) => {
        this.suppressDraftPersist = true;
        this.draftService.clear();
        this.toaster.success('Lijek kreiran preko wizarda.');
        this.router.navigate(['/pharmacy/medications', created.id, 'edit']);
      },
      error: () => {
        this.isSaving = false;
        this.toaster.error('Greška pri čuvanju.');
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/pharmacy/medications']);
  }

  private initializeDraftPrompt(): void {
    const evaluation = this.draftService.evaluateDraft();

    if (evaluation.status === 'none') {
      this.draftPromptResolved = true;
      return;
    }

    if (evaluation.status === 'expired') {
      this.promptExpiredDraftDiscard();
      return;
    }

    this.pendingDraft = evaluation.draft;
    this.showDraftBanner = true;
  }

  private promptExpiredDraftDiscard(): void {
    this.dialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Draft istekao',
        message: 'Sačuvani draft wizarda stariji je od 7 dana i biće odbačen.',
        buttons: [{ type: DialogButton.OK, label: 'U redu', color: 'primary' }],
      })
      .subscribe(() => {
        this.draftService.clear();
        this.draftPromptResolved = true;
      });
  }

  private restoreDraft(draft: MedicationWizardDraft): void {
    const { currentStep, formValue } = buildMedicationWizardDraftRestoreState(draft, this.totalSteps);

    this.form.patchValue(pickMedicationWizardDraftFormPatch(formValue), { emitEvent: false });
    this.step = currentStep;
    this.form.updateValueAndValidity({ emitEvent: false });
  }

  private persistDraft(): void {
    if (this.isSaving || !this.draftPromptResolved) {
      return;
    }

    this.draftService.save(
      buildMedicationWizardDraftSavePayload(this.step, this.form.getRawValue(), this.totalSteps)
    );
  }

  private isStepValid(fields: string[]): boolean {
    let ok = true;
    for (const f of fields) {
      const c = this.form.get(f);
      if (c?.pending) {
        ok = false;
      }
      if (c?.invalid) {
        c.markAsTouched();
        ok = false;
      }
    }
    return ok;
  }
}
