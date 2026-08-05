import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationImageDto, MedicationUpsertCommand } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { medicationNameAsyncValidator } from '../../../shared/validators/medication-name-async.validator';
import {
  extractMedicationImageUploadResponse,
  getHttpUploadProgressPercent,
} from '../utils/medication-image-upload-progress.util';
import { MedicationImageUrlService } from '../../services/medication-image-url.service';

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

  isEditMode = false;
  medicationId: number | null = null;
  isLoading = false;
  isSaving = false;
  images: MedicationImageDto[] = [];
  imageUrls = new Map<number, string>();
  isUploadingImage = false;
  uploadProgress = 0;

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
    this.imageUrlService.revokeAll();
  }

  loadMedication(id: number): void {
    this.isLoading = true;
    this.pharmacyApi.getMedicationById(id).subscribe({
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

    request$.subscribe({
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
    this.pharmacyApi.listImages(id).subscribe({
      next: (imgs) => {
        this.images = imgs;
        this.loadImageUrls(id, imgs);
      },
      error: () => this.toaster.error('Greška pri učitavanju slika.'),
    });
  }

  private loadImageUrls(medicationId: number, images: MedicationImageDto[]): void {
    this.imageUrlService.revokeAll();
    this.imageUrls.clear();

    for (const image of images) {
      this.imageUrlService.getAuthenticatedUrl(medicationId, image.id).subscribe({
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

  onImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || !this.medicationId) return;

    this.isUploadingImage = true;
    this.uploadProgress = 0;
    this.pharmacyApi.uploadImage(this.medicationId, file).subscribe({
      next: (httpEvent) => {
        const progress = getHttpUploadProgressPercent(httpEvent);
        if (progress != null) {
          this.uploadProgress = progress;
        }

        const uploaded = extractMedicationImageUploadResponse(httpEvent);
        if (uploaded) {
          this.isUploadingImage = false;
          this.uploadProgress = 0;
          this.toaster.success('Slika uploadovana.');
          this.loadImages(this.medicationId!);
        }
      },
      error: () => {
        this.isUploadingImage = false;
        this.uploadProgress = 0;
        this.toaster.error('Greška pri uploadu slike.');
      },
    });
  }

  deleteImage(image: MedicationImageDto): void {
    if (!this.medicationId || !confirm('Obrisati sliku?')) return;
    this.pharmacyApi.deleteImage(this.medicationId, image.id).subscribe({
      next: () => {
        this.toaster.success('Slika obrisana.');
        this.loadImages(this.medicationId!);
      },
      error: () => this.toaster.error('Greška pri brisanju slike.'),
    });
  }

  setPrimary(image: MedicationImageDto): void {
    if (!this.medicationId) return;
    this.pharmacyApi.setPrimaryImage(this.medicationId, image.id).subscribe({
      next: () => this.loadImages(this.medicationId!),
      error: () => this.toaster.error('Greška pri postavljanju primarne slike.'),
    });
  }

  imageUrl(image: MedicationImageDto): string | null {
    return this.imageUrls.get(image.id) ?? this.imageUrlService.getLegacyUrl(image.relativeUrl);
  }
}
