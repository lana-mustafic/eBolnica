import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpEventType } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, debounceTime, filter, forkJoin, map, of, Subscription, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationUpsertCommand } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { medicationNameAsyncValidator } from '../../../shared/validators/medication-name-async.validator';
import { DialogButton, DialogType } from '../../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import {
  MedicationWizardDraft,
  MedicationWizardDraftService,
} from '../../services/medication-wizard-draft.service';
import {
  getMedicationCategoryLabel,
  MEDICATION_CATEGORIES,
} from '../../constants/medication-categories.constant';
import { MEDICATION_DOSAGE_FORMS } from '../../constants/medication-dosage-forms.constant';
import {
  buildMedicationWizardDraftRestoreState,
  buildMedicationWizardDraftSavePayload,
  MEDICATION_WIZARD_AUTOSAVE_DEBOUNCE_MS,
  pickMedicationWizardDraftFormPatch,
} from '../medication-wizard-autosave.util';
import { compressMedicationImage } from '../utils/medication-image-compress.util';

interface PendingWizardImage {
  key: string;
  file: File;
  previewUrl: string;
  originalSize: number;
  compressedSize: number;
}

@Component({
  selector: 'app-medication-wizard',
  standalone: false,
  templateUrl: './medication-wizard.component.html',
  styleUrl: './medication-wizard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MedicationWizardComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private pharmacyApi = inject(PharmacyApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private draftService = inject(MedicationWizardDraftService);
  private dialog = inject(DialogHelperService);
  private destroyRef = inject(DestroyRef);

  step = signal(1);
  readonly totalSteps = 3;
  isSaving = signal(false);
  isProcessingQueue = signal(false);
  isDragOver = signal(false);
  showDraftBanner = signal(false);
  pendingDraft = signal<MedicationWizardDraft | null>(null);
  pendingImages = signal<PendingWizardImage[]>([]);

  private autosaveSubscription?: Subscription;
  private draftPromptResolved = false;
  private suppressDraftPersist = false;
  private userStartedAfterDraftPrompt = false;

  categories = [...MEDICATION_CATEGORIES];
  dosageForms = [...MEDICATION_DOSAGE_FORMS];

  readonly categoryLabel = getMedicationCategoryLabel;

  form = this.fb.group({
    name: [
      '',
      [Validators.required, Validators.minLength(3), Validators.maxLength(100)],
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
      .subscribe(() => {
        if (this.showDraftBanner() && !this.draftPromptResolved && !this.userStartedAfterDraftPrompt) {
          this.userStartedAfterDraftPrompt = true;
          this.draftPromptResolved = true;
          this.showDraftBanner.set(false);
          this.pendingDraft.set(null);
        }
        this.persistDraft();
      });
  }

  ngOnDestroy(): void {
    if (this.draftPromptResolved && !this.suppressDraftPersist) {
      this.persistDraft();
    }
    this.autosaveSubscription?.unsubscribe();
    this.clearPendingImagePreviews();
  }

  get nameControl() {
    return this.form.get('name');
  }

  get categoryControl() {
    return this.form.get('category');
  }

  get priceControl() {
    return this.form.get('price');
  }

  get stockQuantityControl() {
    return this.form.get('stockQuantity');
  }

  get minimumStockLevelControl() {
    return this.form.get('minimumStockLevel');
  }

  get expiryDateControl() {
    return this.form.get('expiryDate');
  }

  continueDraft(): void {
    const draft = this.pendingDraft();
    if (!draft) {
      return;
    }

    this.restoreDraft(draft);
    this.showDraftBanner.set(false);
    this.draftPromptResolved = true;
    this.pendingDraft.set(null);
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
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.DELETE) {
          return;
        }

        this.draftService.clear();
        this.pendingDraft.set(null);
        this.showDraftBanner.set(false);
        this.draftPromptResolved = true;
      });
  }

  getDraftSavedAtLabel(): string | null {
    const draft = this.pendingDraft();
    if (!draft?.savedAt) {
      return null;
    }

    const savedAt = new Date(draft.savedAt);
    if (Number.isNaN(savedAt.getTime())) {
      return null;
    }

    return savedAt.toLocaleString('bs-BA', {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  next(): void {
    if (this.step() === 1 && !this.isStepValid(['name', 'category', 'genericName', 'manufacturer', 'description'])) return;
    if (this.step() === 2 && !this.isStepValid(['price', 'stockQuantity', 'minimumStockLevel', 'expiryDate', 'batchNumber', 'dosageForm', 'strength'])) return;
    this.step.update((s) => s + 1);
    this.persistDraft();
  }

  back(): void {
    if (this.step() > 1) {
      this.step.update((s) => s - 1);
      this.persistDraft();
    }
  }

  submit(): void {
    if (this.form.invalid || this.form.pending) {
      this.form.markAllAsTouched();
      this.toaster.error('Provjerite obavezna polja prije čuvanja.');
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

    this.isSaving.set(true);
    this.pharmacyApi
      .createMedication(body)
      .pipe(
        switchMap((created) =>
          this.uploadPendingImages(created.id).pipe(
            map((uploadFailed) => ({ created, uploadFailed }))
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ created, uploadFailed }) => {
          this.suppressDraftPersist = true;
          this.draftService.clear();
          this.clearPendingImagePreviews();
          this.pendingImages.set([]);
          this.toaster.success(
            uploadFailed
              ? 'Lijek kreiran, ali neke slike nisu uploadovane.'
              : 'Lijek kreiran preko wizarda.'
          );
          this.router.navigate(['/pharmacy/medications', created.id]);
        },
        error: (err) => {
          this.isSaving.set(false);
          this.toaster.error(getApiErrorMessage(err, 'Greška pri čuvanju.'));
        },
      });
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    if (!event.dataTransfer?.files?.length) {
      return;
    }
    void this.queueFiles(Array.from(event.dataTransfer.files));
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    input.value = '';
    if (!files?.length) {
      return;
    }
    void this.queueFiles(Array.from(files));
  }

  removePendingImage(key: string): void {
    const item = this.pendingImages().find((entry) => entry.key === key);
    if (item) {
      URL.revokeObjectURL(item.previewUrl);
    }
    this.pendingImages.update((entries) => entries.filter((entry) => entry.key !== key));
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
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

    this.pendingDraft.set(evaluation.draft);
    this.showDraftBanner.set(true);
  }

  private promptExpiredDraftDiscard(): void {
    this.dialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Draft istekao',
        message: 'Sačuvani draft wizarda stariji je od 7 dana i biće odbačen.',
        buttons: [{ type: DialogButton.OK, label: 'U redu', color: 'primary' }],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.draftService.clear();
        this.draftPromptResolved = true;
      });
  }

  private restoreDraft(draft: MedicationWizardDraft): void {
    const { currentStep, formValue } = buildMedicationWizardDraftRestoreState(draft, this.totalSteps);

    this.form.patchValue(pickMedicationWizardDraftFormPatch(formValue), { emitEvent: false });
    this.step.set(currentStep);
    this.form.updateValueAndValidity({ emitEvent: false });
  }

  private persistDraft(): void {
    if (this.isSaving() || !this.draftPromptResolved) {
      return;
    }

    this.draftService.save(
      buildMedicationWizardDraftSavePayload(this.step(), this.form.getRawValue(), this.totalSteps)
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

  private async queueFiles(files: File[]): Promise<void> {
    this.isProcessingQueue.set(true);
    try {
      for (const original of files) {
        if (!original.type.startsWith('image/')) {
          this.toaster.error(`Preskočeno: ${original.name} nije slika.`);
          continue;
        }

        try {
          const compressed = await compressMedicationImage(original);
          this.pendingImages.update((entries) => [
            ...entries,
            {
              key: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
              file: compressed,
              previewUrl: URL.createObjectURL(compressed),
              originalSize: original.size,
              compressedSize: compressed.size,
            },
          ]);
        } catch {
          this.toaster.error(`Greška pri obradi slike: ${original.name}`);
        }
      }
    } finally {
      this.isProcessingQueue.set(false);
    }
  }

  private uploadPendingImages(medicationId: number) {
    const images = this.pendingImages();
    if (images.length === 0) {
      return of(false);
    }

    let uploadFailed = false;
    return forkJoin(
      images.map((item) =>
        this.pharmacyApi.uploadImage(medicationId, item.file).pipe(
          filter((event) => event.type === HttpEventType.Response),
          map(() => undefined),
          catchError(() => {
            uploadFailed = true;
            return of(undefined);
          })
        )
      )
    ).pipe(map(() => uploadFailed));
  }

  private clearPendingImagePreviews(): void {
    for (const item of this.pendingImages()) {
      URL.revokeObjectURL(item.previewUrl);
    }
  }
}
