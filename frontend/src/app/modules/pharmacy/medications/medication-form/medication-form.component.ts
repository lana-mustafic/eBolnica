import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationImageDto, MedicationUpsertCommand } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { medicationNameAsyncValidator } from '../../../shared/validators/medication-name-async.validator';
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
  private destroyRef = inject(DestroyRef);

  isEditMode = false;
  medicationId: number | null = null;
  isLoading = false;
  isSaving = false;
  images: MedicationImageDto[] = [];
  imageUrls = new Map<number, string>();
  pendingUploads: PendingImageUpload[] = [];
  isDragOver = false;
  isProcessingQueue = false;

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

  dosageForms = ['Tablet', 'Capsule', 'Liquid', 'Injection', 'Cream', 'Drops', 'Other'];

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
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam && idParam !== 'new') {
      this.isEditMode = true;
      this.medicationId = Number(idParam);
      this.loadMedication(this.medicationId);
    }
  }

  ngOnDestroy(): void {
    this.clearPendingUploads();
    this.imageUrlService.revokeAll();
  }

  loadMedication(id: number): void {
    this.isLoading = true;
    this.pharmacyApi.getMedicationById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (m) => {
        const expiry = m.expiryDate ? m.expiryDate.split('T')[0] : '';
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
        this.loadImages(id);
      },
      error: () => {
        this.isLoading = false;
        this.toaster.error('Greška pri učitavanju lijeka.');
        this.router.navigate(['/pharmacy/medications']);
      },
    });
  }

  get nameControl() {
    return this.form.get('name');
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
    const request$ =
      this.isEditMode && this.medicationId
        ? this.pharmacyApi.updateMedication(this.medicationId, body)
        : this.pharmacyApi.createMedication(body);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
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
    if (!this.medicationId || !confirm('Obrisati sliku?')) return;
    this.pharmacyApi.deleteImage(this.medicationId, image.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toaster.success('Slika obrisana.');
        this.loadImages(this.medicationId!);
      },
      error: () => this.toaster.error('Greška pri brisanju slike.'),
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
          },
        ];
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
    item.errorMessage = undefined;

    this.pharmacyApi.uploadImage(this.medicationId, item.file).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (httpEvent) => {
        const progress = getHttpUploadProgressPercent(httpEvent);
        if (progress != null) {
          item.progress = progress;
        }

        const uploaded = extractMedicationImageUploadResponse(httpEvent);
        if (uploaded) {
          URL.revokeObjectURL(item.previewUrl);
          this.pendingUploads = this.pendingUploads.filter((entry) => entry.key !== item.key);
          this.toaster.success('Slika uploadovana.');
          this.loadImages(this.medicationId!);
        }
      },
      error: () => {
        item.status = 'error';
        item.progress = 0;
        item.errorMessage = 'Upload nije uspio.';
        this.toaster.error('Greška pri uploadu slike.');
      },
    });
  }

  private loadImageUrls(medicationId: number, images: MedicationImageDto[]): void {
    this.imageUrlService.revokeAll();
    this.imageUrls.clear();

    for (const image of images) {
      this.imageUrlService.getAuthenticatedUrl(medicationId, image.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (url) => this.imageUrls.set(image.id, url),
        error: () => {
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
