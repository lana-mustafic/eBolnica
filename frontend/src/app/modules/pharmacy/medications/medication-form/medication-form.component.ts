import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
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
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY, catchError, map, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto, MedicationImageDto, MedicationUpsertCommand } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthFacadeService } from '../../../../core/services/auth/auth-facade.service';
import { DialogButton, DialogType } from '../../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { medicationNameAsyncValidator } from '../../../shared/validators/medication-name-async.validator';
import {
  MEDICATION_CATEGORIES,
  normalizeMedicationCategory,
} from '../../constants/medication-categories.constant';
import {
  MEDICATION_DOSAGE_FORMS,
  normalizeDosageForm,
} from '../../constants/medication-dosage-forms.constant';
import { MAX_MEDICATION_IMAGES } from '../../constants/medication-image-limits.constant';
import { MedicationImageUrlService } from '../../services/medication-image-url.service';
import {
  MedicationImageLightboxComponent,
  MedicationImageLightboxData,
} from '../medication-image-lightbox/medication-image-lightbox.component';
import { compressMedicationImage } from '../utils/medication-image-compress.util';
import {
  extractMedicationImageUploadResponse,
  getHttpUploadProgressPercent,
} from '../utils/medication-image-upload-progress.util';

interface PendingImageUpload {
  key: string;
  file: File;
  previewUrl: string;
  originalSize: number;
  compressedSize: number;
  status: 'pending' | 'uploading' | 'error';
  progress: number;
  progressKnown: boolean;
  errorMessage?: string;
}

@Component({
  selector: 'app-medication-form',
  standalone: false,
  templateUrl: './medication-form.component.html',
  styleUrl: './medication-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MedicationFormComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private pharmacyApi = inject(PharmacyApiService);
  private imageUrlService = inject(MedicationImageUrlService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private dialog = inject(MatDialog);
  private confirmDialog = inject(DialogHelperService);
  private destroyRef = inject(DestroyRef);

  auth = inject(AuthFacadeService);

  readonly strengthPresets = ['5mg', '10mg', '20mg', '50mg', '100mg', '250mg', '500mg'];
  readonly maxMedicationImages = MAX_MEDICATION_IMAGES;

  medication = signal<MedicationDto | null>(null);
  isEditMode = signal(false);
  medicationId = signal<number | null>(null);
  isLoading = signal(false);
  isSaving = signal(false);
  images = signal<MedicationImageDto[]>([]);
  imageUrls = signal<Map<number, string>>(new Map());
  pendingUploads = signal<PendingImageUpload[]>([]);
  isDragOver = signal(false);
  isProcessingQueue = signal(false);

  private medicationRowVersion: string | null = null;
  private imageLoadGeneration = 0;
  private uploadSessionGeneration = 0;
  private activeUploadCount = 0;

  categories = [...MEDICATION_CATEGORIES];

  dosageForms = [...MEDICATION_DOSAGE_FORMS];

  form = this.fb.group({
    name: [
      '',
      [Validators.required, Validators.minLength(3), Validators.maxLength(100)],
      [
        medicationNameAsyncValidator(
          (name, excludeId) =>
            this.pharmacyApi.checkName(name, excludeId).pipe(
              map((res) => ({ isAvailable: res.isAvailable }))
            ),
          { excludeId: () => this.medicationId() ?? undefined }
        ),
      ],
    ],
    genericName: [''],
    description: [''],
    manufacturer: [''],
    price: [0, [Validators.required, Validators.min(0.01)]],
    stockQuantity: [0, [Validators.required, Validators.min(0)]],
    minimumStockLevel: [10, [Validators.required, Validators.min(0)]],
    expiryDate: ['', Validators.required],
    batchNumber: [''],
    isActive: [true],
    requiresPrescription: [true],
    category: ['', Validators.required],
    dosageForm: [''],
    strength: [''],
  });

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const idParam = params.get('id');
          if (!idParam || idParam === 'new') {
            this.resetNewForm();
            return EMPTY;
          }

          const id = Number(idParam);
          if (!Number.isFinite(id) || id <= 0) {
            this.handleInvalidRouteId();
            return EMPTY;
          }

          this.isEditMode.set(true);
          this.medicationId.set(id);
          this.isLoading.set(true);
          this.medicationRowVersion = null;
          this.medication.set(null);
          this.resetImageSession();

          return this.pharmacyApi.getMedicationById(id).pipe(
            catchError(() => {
              this.isLoading.set(false);
              this.toaster.error('Greška pri učitavanju lijeka.');
              this.router.navigate(['/pharmacy/medications']);
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((medication) => {
        this.applyMedication(medication);
      });
  }

  ngOnDestroy(): void {
    this.clearPendingUploads();
    this.imageUrlService.revokeAll();
  }

  private resetNewForm(): void {
    this.isEditMode.set(false);
    this.medicationId.set(null);
    this.medicationRowVersion = null;
    this.medication.set(null);
    this.isLoading.set(false);
    this.form.reset({
      name: '',
      genericName: '',
      description: '',
      manufacturer: '',
      price: 0,
      stockQuantity: 0,
      minimumStockLevel: 10,
      expiryDate: '',
      batchNumber: '',
      isActive: true,
      requiresPrescription: true,
      category: '',
      dosageForm: '',
      strength: '',
    });
    this.resetImageSession();
  }

  private resetImageSession(): void {
    this.uploadSessionGeneration++;
    this.imageLoadGeneration++;
    this.activeUploadCount = 0;
    this.clearPendingUploads();
    this.imageUrlService.revokeAll();
    this.imageUrls.set(new Map());
    this.images.set([]);
  }

  private reloadMedication(id: number): void {
    this.isLoading.set(true);
    this.pharmacyApi
      .getMedicationById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (medication) => this.applyMedication(medication),
        error: () => {
          this.isLoading.set(false);
          this.toaster.error('Greška pri osvježavanju lijeka.');
        },
      });
  }

  private handleInvalidRouteId(): void {
    this.isEditMode.set(false);
    this.medicationId.set(null);
    this.medicationRowVersion = null;
    this.medication.set(null);
    this.isLoading.set(false);
    this.toaster.error('Neispravan ID lijeka.');
    this.router.navigate(['/pharmacy/medications']);
  }

  private applyMedication(m: MedicationDto): void {
    const expiry = m.expiryDate ? m.expiryDate.split('T')[0] : '';
    this.medication.set(m);
    this.medicationRowVersion = m.rowVersion ?? null;
    this.form.patchValue({
      name: m.name,
      genericName: m.genericName ?? '',
      description: m.description ?? '',
      manufacturer: m.manufacturer ?? '',
      price: m.price,
      stockQuantity: m.stockQuantity,
      minimumStockLevel: m.minimumStockLevel,
      expiryDate: expiry,
      batchNumber: m.batchNumber ?? '',
      isActive: m.isActive,
      requiresPrescription: m.requiresPrescription,
      category: normalizeMedicationCategory(m.category),
      dosageForm: normalizeDosageForm(m.dosageForm),
      strength: m.strength ?? '',
    });
    this.isLoading.set(false);
    this.loadImages(m.id);
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

  get isFormReady(): boolean {
    return this.form.valid && !this.form.pending;
  }

  get missingRequiredCount(): number {
    const keys = ['name', 'category', 'price', 'stockQuantity', 'minimumStockLevel', 'expiryDate'];
    return keys.filter((key) => this.form.get(key)?.invalid).length;
  }

  get imageSlotsUsed(): number {
    return (
      this.images().length +
      this.pendingUploads().filter((entry) => entry.status !== 'error').length
    );
  }

  get canAddMoreImages(): boolean {
    return this.imageSlotsUsed < MAX_MEDICATION_IMAGES;
  }

  get remainingImageSlots(): number {
    return Math.max(0, MAX_MEDICATION_IMAGES - this.imageSlotsUsed);
  }

  deleteMedication(): void {
    const id = this.medicationId();
    if (!this.isEditMode() || !id) {
      return;
    }

    const name = this.form.get('name')?.value ?? 'lijek';

    this.confirmDialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Deaktiviraj lijek',
        message: `Jeste li sigurni da želite deaktivirati lijek "${name}"?`,
        buttons: [
          { type: DialogButton.CANCEL, label: 'Odustani' },
          { type: DialogButton.DELETE, label: 'Deaktiviraj', color: 'warn' },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.DELETE || !this.medicationId()) {
          return;
        }

        this.pharmacyApi
          .deleteMedication(this.medicationId()!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.toaster.success('Lijek deaktiviran.');
              this.router.navigate(['/pharmacy/medications']);
            },
            error: (err) => {
              this.toaster.error(getApiErrorMessage(err, 'Greška pri deaktivaciji lijeka.'));
            },
          });
      });
  }

  submit(): void {
    if (this.form.invalid || this.form.pending) {
      this.form.markAllAsTouched();
      this.toaster.error('Provjerite obavezna polja prije čuvanja.');
      return;
    }

    const id = this.medicationId();
    if (this.isEditMode() && id && !this.medicationRowVersion) {
      this.toaster.error('Verzija lijeka nije učitana. Osvježavam podatke...');
      this.reloadMedication(id);
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
      rowVersion: this.isEditMode() ? this.medicationRowVersion! : null,
    };

    this.isSaving.set(true);
    const request$ =
      this.isEditMode() && id
        ? this.pharmacyApi.updateMedication(id, body)
        : this.pharmacyApi.createMedication(body);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        if (saved?.rowVersion) {
          this.medicationRowVersion = saved.rowVersion;
        }
        this.toaster.success(this.isEditMode() ? 'Lijek ažuriran.' : 'Lijek kreiran.');
        this.router.navigate(['/pharmacy/medications']);
      },
      error: (err) => {
        this.isSaving.set(false);
        const message =
          err?.status === 409
            ? 'Lijek je u međuvremenu izmijenjen. Podaci su osvježeni — provjerite i pokušajte ponovo.'
            : getApiErrorMessage(err, 'Greška pri čuvanju lijeka.');
        this.toaster.error(message);
        if (err?.status === 409 && id) {
          this.reloadMedication(id);
        }
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/pharmacy/medications']);
  }

  loadImages(id: number): void {
    const generation = ++this.imageLoadGeneration;
    this.pharmacyApi.listImages(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (imgs) => {
        if (generation !== this.imageLoadGeneration || this.medicationId() !== id) {
          return;
        }
        this.images.set(imgs);
        this.loadImageUrls(id, imgs);
      },
      error: () => this.toaster.error('Greška pri učitavanju slika.'),
    });
  }

  onDragOver(event: DragEvent): void {
    if (!this.canAddMoreImages) {
      return;
    }
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
    if (!this.medicationId() || !event.dataTransfer?.files?.length || !this.canAddMoreImages) {
      if (!this.canAddMoreImages) {
        this.toaster.warning(`Maksimalno ${MAX_MEDICATION_IMAGES} slika po lijeku.`);
      }
      return;
    }
    void this.queueFiles(Array.from(event.dataTransfer.files));
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    input.value = '';
    if (!files?.length || !this.medicationId()) {
      return;
    }
    if (!this.canAddMoreImages) {
      this.toaster.warning(`Maksimalno ${MAX_MEDICATION_IMAGES} slika po lijeku.`);
      return;
    }
    void this.queueFiles(Array.from(files));
  }

  removePending(key: string): void {
    const item = this.pendingUploads().find((entry) => entry.key === key);
    if (item) {
      URL.revokeObjectURL(item.previewUrl);
    }
    this.pendingUploads.update((entries) => entries.filter((entry) => entry.key !== key));
  }

  uploadPending(item: PendingImageUpload): void {
    if (!this.medicationId() || item.status === 'uploading' || !this.canAddMoreImages) {
      return;
    }
    this.uploadFile(item);
  }

  uploadAllPending(): void {
    if (!this.canAddMoreImages) {
      this.toaster.warning(`Maksimalno ${MAX_MEDICATION_IMAGES} slika po lijeku.`);
      return;
    }
    for (const item of this.pendingUploads().filter((entry) => entry.status !== 'uploading')) {
      if (!this.canAddMoreImages) {
        break;
      }
      this.uploadFile(item);
    }
  }

  retryPending(item: PendingImageUpload): void {
    item.status = 'pending';
    item.progress = 0;
    item.errorMessage = undefined;
    this.pendingUploads.update((entries) => [...entries]);
    this.uploadFile(item);
  }

  openLightbox(image: MedicationImageDto): void {
    const url = this.imageUrl(image);
    if (!url) {
      return;
    }

    this.dialog.open(MedicationImageLightboxComponent, {
      data: { imageUrl: url, fileName: image.fileName } satisfies MedicationImageLightboxData,
      maxWidth: '95vw',
      panelClass: 'medication-image-lightbox-panel',
    });
  }

  dropGallery(event: CdkDragDrop<MedicationImageDto[]>): void {
    if (event.previousIndex === event.currentIndex || !this.medicationId()) {
      return;
    }

    const reordered = [...this.images()];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);
    this.images.set(reordered);

    const imageIds = reordered.map((image) => image.id);
    const id = this.medicationId()!;
    this.pharmacyApi.reorderImages(id, imageIds).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.toaster.success('Redoslijed slika ažuriran.'),
      error: () => {
        this.toaster.error('Greška pri reorderu slika.');
        this.loadImages(id);
      },
    });
  }

  deleteImage(image: MedicationImageDto): void {
    const id = this.medicationId();
    if (!id) return;

    this.confirmDialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Obriši sliku',
        message: 'Jeste li sigurni da želite obrisati ovu sliku?',
        buttons: [
          { type: DialogButton.CANCEL, label: 'Odustani' },
          { type: DialogButton.DELETE, label: 'Obriši', color: 'warn' },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.DELETE || !this.medicationId()) {
          return;
        }

        this.pharmacyApi.deleteImage(this.medicationId()!, image.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            this.toaster.success('Slika obrisana.');
            this.loadImages(this.medicationId()!);
          },
          error: () => this.toaster.error('Greška pri brisanju slike.'),
        });
      });
  }

  setPrimary(image: MedicationImageDto): void {
    const id = this.medicationId();
    if (!id) return;
    this.pharmacyApi.setPrimaryImage(id, image.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.loadImages(id),
      error: () => this.toaster.error('Greška pri postavljanju primarne slike.'),
    });
  }

  imageUrl(image: MedicationImageDto): string | null {
    return this.imageUrls().get(image.id) ?? null;
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  private async queueFiles(files: File[]): Promise<void> {
    if (!this.medicationId()) {
      return;
    }

    if (!this.canAddMoreImages) {
      this.toaster.warning(`Maksimalno ${MAX_MEDICATION_IMAGES} slika po lijeku.`);
      return;
    }

    this.isProcessingQueue.set(true);
    try {
      for (const original of files) {
        if (this.imageSlotsUsed >= MAX_MEDICATION_IMAGES) {
          this.toaster.warning(`Dodano do limita od ${MAX_MEDICATION_IMAGES} slika.`);
          break;
        }

        if (!original.type.startsWith('image/')) {
          this.toaster.error(`Preskočeno: ${original.name} nije slika.`);
          continue;
        }

        try {
          const compressed = await compressMedicationImage(original);
          const previewUrl = URL.createObjectURL(compressed);
          this.pendingUploads.update((entries) => [
            ...entries,
            {
              key: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
              file: compressed,
              previewUrl,
              originalSize: original.size,
              compressedSize: compressed.size,
              status: 'pending',
              progress: 0,
              progressKnown: false,
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

  private uploadFile(item: PendingImageUpload): void {
    const targetMedicationId = this.medicationId();
    if (!targetMedicationId) {
      return;
    }

    if (this.images().length >= MAX_MEDICATION_IMAGES) {
      item.status = 'error';
      item.progress = 0;
      item.progressKnown = false;
      item.errorMessage = `Limit od ${MAX_MEDICATION_IMAGES} slika je dostignut.`;
      this.pendingUploads.update((entries) => [...entries]);
      this.toaster.warning(item.errorMessage);
      return;
    }

    const uploadSession = this.uploadSessionGeneration;

    item.status = 'uploading';
    item.progress = 0;
    item.progressKnown = false;
    item.errorMessage = undefined;
    this.pendingUploads.update((entries) => [...entries]);

    this.activeUploadCount++;
    this.pharmacyApi.uploadImage(targetMedicationId, item.file).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (httpEvent) => {
        const progress = getHttpUploadProgressPercent(httpEvent);
        if (progress != null && uploadSession === this.uploadSessionGeneration) {
          item.progress = progress;
          item.progressKnown = true;
          this.pendingUploads.update((entries) => [...entries]);
        }

        const uploaded = extractMedicationImageUploadResponse(httpEvent);
        if (!uploaded) {
          return;
        }

        URL.revokeObjectURL(item.previewUrl);
        this.pendingUploads.update((entries) => entries.filter((entry) => entry.key !== item.key));
        if (uploadSession === this.uploadSessionGeneration) {
          this.toaster.success('Slika uploadovana.');
        }
        this.finishUploadBatch(targetMedicationId, uploadSession);
      },
      error: () => {
        if (uploadSession === this.uploadSessionGeneration) {
          item.status = 'error';
          item.progress = 0;
          item.progressKnown = false;
          item.errorMessage = 'Upload nije uspio.';
          this.pendingUploads.update((entries) => [...entries]);
          this.toaster.error('Greška pri uploadu slike.');
        }
        this.finishUploadBatch(targetMedicationId, uploadSession);
      },
    });
  }

  private finishUploadBatch(targetMedicationId: number, uploadSession: number): void {
    this.activeUploadCount = Math.max(0, this.activeUploadCount - 1);
    if (this.activeUploadCount !== 0) {
      return;
    }

    if (
      uploadSession !== this.uploadSessionGeneration
      || this.medicationId() !== targetMedicationId
    ) {
      return;
    }

    this.loadImages(targetMedicationId);
  }

  private loadImageUrls(medicationId: number, images: MedicationImageDto[]): void {
    const generation = ++this.imageLoadGeneration;
    this.imageUrlService.revokeAll();
    this.imageUrls.set(new Map());

    for (const image of images) {
      this.imageUrlService
        .getAuthenticatedUrl(medicationId, image.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (url) => {
            if (generation !== this.imageLoadGeneration) {
              return;
            }
            this.imageUrls.update((map) => {
              const next = new Map(map);
              next.set(image.id, url);
              return next;
            });
          },
          error: () => {
            if (generation !== this.imageLoadGeneration) {
              return;
            }
          },
        });
    }
  }

  private clearPendingUploads(): void {
    for (const item of this.pendingUploads()) {
      URL.revokeObjectURL(item.previewUrl);
    }
    this.pendingUploads.set([]);
  }
}
