import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY, catchError, map, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto, MedicationImageDto, MedicationUpsertCommand } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { DialogButton, DialogType } from '../../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { medicationNameAsyncValidator } from '../../../shared/validators/medication-name-async.validator';
import { MEDICATION_DOSAGE_FORMS } from '../../constants/medication-dosage-forms.constant';
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

  isEditMode = false;
  medicationId: number | null = null;
  medicationRowVersion: string | null = null;
  isLoading = false;
  isSaving = false;
  images: MedicationImageDto[] = [];
  imageUrls = new Map<number, string>();
  pendingUploads: PendingImageUpload[] = [];
  isDragOver = false;
  isProcessingQueue = false;
  private imageLoadGeneration = 0;
  private activeUploadCount = 0;

  categories = [
    'Analgesics',
    'Antibiotics',
    'Antivirals',
    'Cardiovascular',
    'Diabetes',
    'Gastrointestinal',
    'Respiratory',
    'Vitamins',
    'Other',
  ];

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
          { excludeId: () => this.medicationId ?? undefined }
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

          this.isEditMode = true;
          this.medicationId = id;
          this.isLoading = true;
          this.medicationRowVersion = null;
          this.images = [];
          this.imageUrls.clear();
          this.clearPendingUploads();

          return this.pharmacyApi.getMedicationById(id).pipe(
            catchError(() => {
              this.isLoading = false;
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
    this.isEditMode = false;
    this.medicationId = null;
    this.medicationRowVersion = null;
    this.isLoading = false;
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
    this.images = [];
    this.imageUrls.clear();
    this.clearPendingUploads();
  }

  private handleInvalidRouteId(): void {
    this.isEditMode = false;
    this.medicationId = null;
    this.medicationRowVersion = null;
    this.isLoading = false;
    this.toaster.error('Neispravan ID lijeka.');
    this.router.navigate(['/pharmacy/medications']);
  }

  private applyMedication(m: MedicationDto): void {
    const expiry = m.expiryDate ? m.expiryDate.split('T')[0] : '';
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
      category: m.category ?? '',
      dosageForm: m.dosageForm ?? '',
      strength: m.strength ?? '',
    });
    this.isLoading = false;
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
      rowVersion: this.isEditMode ? this.medicationRowVersion : null,
    };

    this.isSaving = true;
    const request$ =
      this.isEditMode && this.medicationId
        ? this.pharmacyApi.updateMedication(this.medicationId, body)
        : this.pharmacyApi.createMedication(body);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        if (saved?.rowVersion) {
          this.medicationRowVersion = saved.rowVersion;
        }
        this.toaster.success(this.isEditMode ? 'Lijek ažuriran.' : 'Lijek kreiran.');
        this.router.navigate(['/pharmacy/medications']);
      },
      error: (err) => {
        this.isSaving = false;
        const msg = err?.error?.message ?? 'Greška pri čuvanju lijeka.';
        this.toaster.error(msg);
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/pharmacy/medications']);
  }

  loadImages(id: number): void {
    this.pharmacyApi.listImages(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (imgs) => {
        this.images = imgs;
        this.loadImageUrls(id, imgs);
      },
      error: () => this.toaster.error('Greška pri učitavanju slika.'),
    });
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
    if (!this.medicationId || !event.dataTransfer?.files?.length) {
      return;
    }
    void this.queueFiles(Array.from(event.dataTransfer.files));
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    input.value = '';
    if (!files?.length || !this.medicationId) {
      return;
    }
    void this.queueFiles(Array.from(files));
  }

  removePending(key: string): void {
    const item = this.pendingUploads.find((entry) => entry.key === key);
    if (item) {
      URL.revokeObjectURL(item.previewUrl);
    }
    this.pendingUploads = this.pendingUploads.filter((entry) => entry.key !== key);
  }

  uploadPending(item: PendingImageUpload): void {
    if (!this.medicationId || item.status === 'uploading') {
      return;
    }
    this.uploadFile(item);
  }

  uploadAllPending(): void {
    for (const item of this.pendingUploads.filter((entry) => entry.status !== 'uploading')) {
      this.uploadFile(item);
    }
  }

  retryPending(item: PendingImageUpload): void {
    item.status = 'pending';
    item.progress = 0;
    item.errorMessage = undefined;
    this.uploadFile(item);
  }

  openLightbox(image: MedicationImageDto): void {
    const imageUrl = this.imageUrl(image);
    if (!imageUrl) {
      return;
    }

    this.dialog.open(MedicationImageLightboxComponent, {
      data: { imageUrl, fileName: image.fileName } satisfies MedicationImageLightboxData,
      maxWidth: '95vw',
      panelClass: 'medication-image-lightbox-panel',
    });
  }

  dropGallery(event: CdkDragDrop<MedicationImageDto[]>): void {
    if (event.previousIndex === event.currentIndex || !this.medicationId) {
      return;
    }

    moveItemInArray(this.images, event.previousIndex, event.currentIndex);
    const imageIds = this.images.map((image) => image.id);
    this.pharmacyApi.reorderImages(this.medicationId, imageIds).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.toaster.success('Redoslijed slika ažuriran.'),
      error: () => {
        this.toaster.error('Greška pri reorderu slika.');
        this.loadImages(this.medicationId!);
      },
    });
  }

  deleteImage(image: MedicationImageDto): void {
    if (!this.medicationId) return;

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
        if (result?.button !== DialogButton.DELETE || !this.medicationId) {
          return;
        }

        this.pharmacyApi.deleteImage(this.medicationId, image.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            this.toaster.success('Slika obrisana.');
            this.loadImages(this.medicationId!);
          },
          error: () => this.toaster.error('Greška pri brisanju slike.'),
        });
      });
  }

  setPrimary(image: MedicationImageDto): void {
    if (!this.medicationId) return;
    this.pharmacyApi.setPrimaryImage(this.medicationId, image.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.loadImages(this.medicationId!),
      error: () => this.toaster.error('Greška pri postavljanju primarne slike.'),
    });
  }

  imageUrl(image: MedicationImageDto): string | null {
    return this.imageUrls.get(image.id) ?? this.imageUrlService.getLegacyUrl(image.relativeUrl);
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  private async queueFiles(files: File[]): Promise<void> {
    if (!this.medicationId) {
      return;
    }

    this.isProcessingQueue = true;
    try {
      for (const original of files) {
        if (!original.type.startsWith('image/')) {
          this.toaster.error(`Preskočeno: ${original.name} nije slika.`);
          continue;
        }

        try {
          const compressed = await compressMedicationImage(original);
          const previewUrl = URL.createObjectURL(compressed);
          this.pendingUploads = [
            ...this.pendingUploads,
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
          ];
        } catch {
          this.toaster.error(`Greška pri obradi slike: ${original.name}`);
        }
      }
    } finally {
      this.isProcessingQueue = false;
    }
  }

  private uploadFile(item: PendingImageUpload): void {
    if (!this.medicationId) {
      return;
    }

    item.status = 'uploading';
    item.progress = 0;
    item.progressKnown = false;
    item.errorMessage = undefined;

    this.activeUploadCount++;
    this.pharmacyApi.uploadImage(this.medicationId, item.file).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (httpEvent) => {
        const progress = getHttpUploadProgressPercent(httpEvent);
        if (progress != null) {
          item.progress = progress;
          item.progressKnown = true;
        }

        const uploaded = extractMedicationImageUploadResponse(httpEvent);
        if (uploaded) {
          URL.revokeObjectURL(item.previewUrl);
          this.pendingUploads = this.pendingUploads.filter((entry) => entry.key !== item.key);
          this.toaster.success('Slika uploadovana.');
          this.finishUploadBatch();
        }
      },
      error: () => {
        item.status = 'error';
        item.progress = 0;
        item.progressKnown = false;
        item.errorMessage = 'Upload nije uspio.';
        this.toaster.error('Greška pri uploadu slike.');
        this.finishUploadBatch();
      },
    });
  }

  private finishUploadBatch(): void {
    this.activeUploadCount = Math.max(0, this.activeUploadCount - 1);
    if (this.activeUploadCount === 0 && this.medicationId) {
      this.loadImages(this.medicationId);
    }
  }

  private loadImageUrls(medicationId: number, images: MedicationImageDto[]): void {
    const generation = ++this.imageLoadGeneration;
    this.imageUrlService.revokeAll();
    this.imageUrls.clear();

    for (const image of images) {
      this.imageUrlService
        .getAuthenticatedUrl(medicationId, image.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (url) => {
            if (generation !== this.imageLoadGeneration) {
              return;
            }
            this.imageUrls.set(image.id, url);
          },
          error: () => {
            if (generation !== this.imageLoadGeneration) {
              return;
            }
            const legacy = this.imageUrlService.getLegacyUrl(image.relativeUrl);
            if (legacy) {
              this.imageUrls.set(image.id, legacy);
            }
          },
        });
    }
  }

  private clearPendingUploads(): void {
    for (const item of this.pendingUploads) {
      URL.revokeObjectURL(item.previewUrl);
    }
    this.pendingUploads = [];
  }
}
